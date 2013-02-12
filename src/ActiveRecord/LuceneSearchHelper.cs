#region using

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Scopes;
using Castle.Core.Internal;
using Castle.Core.Logging;
using Dry.Common;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Impl;
using NHibernate.Search;
using NHibernate.Search.Attributes;
using NHibernate.Search.Backend;
using NHibernate.Search.Engine;
using NHibernate.Search.Event;
using NHibernate.Search.Impl;
using NHibernate.Search.Query;
using ServiceStack.Text;
using Dry.Common.Model;
using Environment = System.Environment;
using System.Data;

#endregion

namespace Dry.Common.ActiveRecord {
    public static class LuceneSearchHelper {
        public static readonly string[] Escape = new[] { " ", "+", "-", "&&", "||", "!", "(", ")", "{", "}", "[", "]", "^", "~", "*", "?", ":", "\\", "\"", ",", "/", "." };
        public static readonly string[] Nonsearchable = new[] { "any", "the", "and", "of", "his", "at", "my", "when", "there", "is", "as", "are", "so", "or", "it", "&"};

        public static Dictionary<string, string> Synonyms {
                get {
                    var syn = Settings.Get("synonyms");
                    return syn != null
                        ? JsonSerializer.DeserializeFromString<Dictionary<string, string>>(syn.ToString())
                        : new Dictionary<string, string>();
                }
        }

        public static string ParseTerm(this string phrase) {
            if (string.IsNullOrEmpty(phrase)) return null;
            var terms = phrase.ToLower().SplitAndTrim(Escape).ToList();
            foreach(var non in Nonsearchable) {
                terms.Remove(non);
            }
            var syns = Synonyms;
            return terms.Select(s => syns.ContainsKey(s)
                ? "+(" + s + " (" + syns[s].ParseTerm() + "))"
                : "+" + s + "*").Join(" ");
        }

        public static IFullTextSession CreateLuceneSession(this ISession session) {
            return NHibernate.Search.Search.CreateFullTextSession(session);
        }

        private const int BatchSize = 500;

        public static Lucene.Net.Util.Version LuceneVersion = Lucene.Net.Util.Version.LUCENE_29;
        public static IndexWriter.MaxFieldLength Maxfieldlength = new IndexWriter.MaxFieldLength(IndexWriter.DEFAULT_MAX_FIELD_LENGTH);
        public static readonly StandardAnalyzer Analyzer = new StandardAnalyzer(LuceneVersion);

#if DEBUG
        static readonly ProducerConsumerQueue Indexqueue = new ProducerConsumerQueue("Lucene Indexer", 1);
#else
        static readonly ProducerConsumerQueue Indexqueue = new ProducerConsumerQueue("Lucene Indexer", Environment.ProcessorCount);
#endif

        public static void Index() {
            AR.Holder.GetRegisteredTypes().Where(t => t.HasAttribute<IndexedAttribute>())
                .ForAll(type => Indexqueue.Enqueue(() => Index(type)));
        }

        public static void Index(Type type) {
            using (var session = AR.Holder.GetSessionFactory(type).OpenStatelessSession()) {
                var ids = session.CreateCriteria(type)
                    .AddOrder(new Order(Projections.Id(), false))
                    .SetProjection(Projections.Id())
                    .List<object>();

                Parallel.ForEach(ids.Partition(BatchSize), i => Indexqueue.Enqueue(() => IndexObject(type, i)));
            }
        }

        public static void BeginIndexObject(Type type, params object[] ids) {
            if (ids.HasItems())
                Indexqueue.Enqueue(() => IndexObject(type, ids));
        }

        static void IndexObject(Type type, IEnumerable<object> ids) {
            var works = GenerateLuceneWorks(type, ids);
            Indexqueue.Enqueue(() => MergeIndexes(type, works));
        }

        public static IEnumerable<object> GetIds(ISession session, Type type, Query query) {
            return GetIds(session.GetSessionImplementation(), type, query);
        }

        public static IEnumerable<object> GetIds(IStatelessSession session, Type type, Query query) {
            return GetIds(session.GetSessionImplementation(), type, query);
        }

        public static IEnumerable<object> GetIds(Type type, Query query) {
            var sf = type.GetSearchFactory();
            return GetIds(sf, type, query);
        }

        public static IEnumerable<object> GetIds(ISessionImplementor session, Type type, Query query) {
            var sf = session.GetSearchFactory();
            return GetIds(sf, type, query);
        }

        public static IEnumerable<object> GetIds(SearchFactoryImpl sf, Type type, Query query) {
            Iesi.Collections.Generic.ISet<Type> types;
            var searcher = FullTextSearchHelper.BuildSearcher(sf, out types, type);
            if (searcher == null)
                throw new SearchException("Could not find a searcher for class: " + type.FullName);

            try {
                var q = FullTextSearchHelper.FilterQueryByClasses(types, query);
                return GetIds(sf, searcher, type, q);
            }
            finally {
                searcher.Close();
            }
        }

        public static IEnumerable<object> GetIds(SearchFactoryImpl sf, IndexSearcher searcher, Type type, Query query) {
            var doccount = searcher.GetIndexReader().MaxDoc();
            if (doccount == 0) return new List<object>();
            var docs = searcher.Search(query, doccount);
            return docs.ScoreDocs.Select(t => DocumentBuilder.GetDocumentId(sf, type, searcher.Doc(t.doc)));

        }

        public static void MergeIndexes(Type type, IList<LuceneWork> works) {
            using (var session = AR.Holder.GetSessionFactory(type).OpenSession()) {
                session.BeginTransaction(IsolationLevel.ReadUncommitted);
                session.FlushMode = FlushMode.Never;
                try {
                    var sf = session.GetSearchFactory();
                    sf.BackendQueueProcessorFactory.GetProcessor(works)(null);
                } catch (Exception e) {
                    IoC.Resolve<ILogger>().Error("Error while generating temporary lucene index.", e);
                }
            }
        }

        public static IList<LuceneWork> GenerateLuceneWorks(Type type, IEnumerable<object> ids) {
            using (var session = AR.Holder.GetSessionFactory(type).OpenSession()) {
                session.BeginTransaction(IsolationLevel.ReadUncommitted);
                session.FlushMode = FlushMode.Never;
                try {
                    var sf = session.GetSearchFactory();
                    var docbuilder = sf.GetDocumentBuilder(type);
                    return GetInstances(type, session, ids).SelectMany(instance => GenerateInstanceWorks(session.GetSessionImplementation(), type, instance, docbuilder)).ToList();
                } catch (Exception e) {
                    IoC.Resolve<ILogger>().Error("Error while generating temporary lucene index.", e);
                }
            }
            return new List<LuceneWork>();
        }

        public static IEnumerable<LuceneWork> GenerateInstanceWorks(ISessionImplementor session, Type type, object instance, DocumentBuilder docbuilder) {
            var p = session.GetEntityPersister(type.FullName, instance);
            var id = p.GetIdentifier(instance, EntityMode.Poco);
            return new LuceneWork[] {
                new DeleteLuceneWork(id, id.ToString(), type),
                new AddLuceneWork(id, id.ToString(), type, docbuilder.GetDocument(instance, id, type)) {IsBatch = true}
            };
        }

        static IEnumerable<object> GetInstances(Type type, ISession session, IEnumerable<object> ids) {
            // var model = AR.Holder.GetModel(type);
            return ids.Partition(BatchSize).SelectMany(i => {
                if (i == null) return new object[]{};

                var q = session.CreateCriteria(type)
                    .Add(Restrictions.In(Projections.Id(), i.ToArray()));

                // model.BelongsTos.Keys.ForAll(prop => q.SetFetchMode(prop, FetchMode.Eager));

                return q.List<object>();
            });
        }

        public static void Optimize() {
            AR.Holder.GetRegisteredTypes().Where(t => t.HasAttribute<IndexedAttribute>())
                .ForAll(type => Indexqueue.Enqueue(() => Index(type)));
        }

        public static void Optimize(Type type) {
            Indexqueue.Enqueue(() => InternalOptimize(type));
        }

        static void InternalOptimize(Type type) {
            type.GetSearchFactory().Optimize(type);
        }

        public static Type[] GetIndexedTypes() {
            return AR.Holder.GetRegisteredTypes().Where(t => t.HasAttribute<IndexedAttribute>())
                .OrderBy(t => t.FullName)
                .ToArray();
        }

        public static SearchFactoryImpl GetSearchFactory(this ISession s) {
            return GetSearchFactory(s.GetSessionImplementation());
        }

        public static SearchFactoryImpl GetSearchFactory(this IStatelessSession s) {
            return GetSearchFactory(s.GetSessionImplementation());
        }

        public static SearchFactoryImpl GetSearchFactory(this ISessionImplementor s) {
            var listeners = s.Listeners.PostInsertEventListeners;
            return GetSearchFactory(listeners);
        }


        public static SearchFactoryImpl GetSearchFactory(this Type type) {
            var cfg = AR.Holder.GetConfiguration(type);
            return GetSearchFactory(cfg.EventListeners.PostInsertEventListeners);
        }

        private static SearchFactoryImpl GetSearchFactory(IEnumerable listeners) {
            FullTextIndexEventListener listener = null;

            // this sucks since the event listener use is mandated
            foreach (var eventListener in listeners.OfType<FullTextIndexEventListener>()) {
                listener = eventListener;
                return listener.SearchFactory as SearchFactoryImpl;
            }

            throw new HibernateException(
                "Hibernate Search Event listeners not configured, please check the reference documentation and the "
                + "application's hibernate.cfg.xml");
        }

        public  static IndexWriter GetIndexWriter(this Directory dir) {
            return dir.GetIndexWriter(true);
        }

        public static IndexWriter GetIndexWriter(this Directory dir, bool create) {
            var analyser = new StandardAnalyzer(LuceneVersion);
            var writer = new IndexWriter(dir, analyser, create, Maxfieldlength);
            return writer;
        }

        public static IndexReader GetIndexReader(this Directory dir) {
            return IndexReader.Open(dir, true);
        }

        public static IndexReader GetIndexReader(this Directory dir, bool ronly) {
            return IndexReader.Open(dir, ronly);
        }

        public static string[] GetSearchableProperties(Type type) {
            var sf = type.GetSearchFactory();
            var fieldlist = new HashSet<string>();
            foreach(var dir in sf.GetDirectoryProviders(type)) {
                var reader = dir.Directory.GetIndexReader();
                try {
                    var searchfields = reader.GetFieldNames(IndexReader.FieldOption.ALL);
                    fieldlist.AddRange(searchfields.Where(f => !f.StartsWith("_")));
                } finally {
                    if (reader != null)
                        reader.Close();
                }
            }
            return fieldlist.ToArray();
        }

        public static void DeleteIndex(Type type) {
            var sf = type.GetSearchFactory();
            foreach(var dirprovider in sf.GetDirectoryProviders(type)) {
                var lockobj = sf.GetLockableDirectoryProviders()[dirprovider];
                lock(lockobj) {
                    var writer = dirprovider.Directory.GetIndexWriter();
                    try {
                        writer.DeleteAll();
                        writer.Commit();
                    } catch (Exception e) {
                        IoC.Resolve<ILogger>().Error("Could not delete index folder:", e);
                    } finally {
                        writer.Close();
                    }
                }
            }
        }
    }
}
