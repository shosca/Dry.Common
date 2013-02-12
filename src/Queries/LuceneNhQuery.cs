#region using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.Components.Binder;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using NHibernate;
using NHibernate.Impl;
using NHibernate.Metadata;
using NHibernate.Type;
using Dry.Common.ActiveRecord;

#endregion

namespace Dry.Common.Queries {
    public class LuceneNhQuery<T> : AbstactQuery<T> where T : class {
        public IList<string> Fields = LuceneSearchHelper.GetSearchableProperties(typeof (T));
        protected BooleanQuery BaseQuery = new BooleanQuery();
        protected bool DoOrderby { get; set; }
        readonly Analyzer _analyser = new StandardAnalyzer(LuceneSearchHelper.LuceneVersion);
        public IClassMetadata Metadata { get; private set; }


        public LuceneNhQuery() {
            DoOrderby = false;
            Metadata = AR.Holder.GetClassMetadata(Type);
        }

        void GenerateQuery(CompositeNode rootnode) {
            RootNode = rootnode;
            var version = LuceneSearchHelper.LuceneVersion;
            var analyser = new StandardAnalyzer(version);

            var props = Fields.Where(s => !s.EndsWith(".Id") && s != "Id").ToArray();

            var termsphrase = rootnode.GetParameter("term");
            var terms = termsphrase.ParseTerm();

            if (!string.IsNullOrEmpty(terms)) {
                Querystring["term"] = termsphrase;
                var query = new MultiFieldQueryParser(version, props, analyser).Parse(terms);
                BaseQuery.Add(query, BooleanClause.Occur.MUST);
            }

            var anys = rootnode.GetParameter("any").SplitAndTrim(LuceneSearchHelper.Escape)
                .Select(s => s.Replace("\"", ""));
            if (anys.HasItems()) {
                Querystring["any"] = anys.Join(" ");
                var any = anys.Select(s => s + "~").Join(" ");
                var query = new MultiFieldQueryParser(version, props, analyser).Parse(any);
                BaseQuery.Add(query, BooleanClause.Occur.SHOULD);
                Querystring["any"] = any;
            }

            var exacts = rootnode.GetParameter("exact").SplitAndTrim(LuceneSearchHelper.Escape)
                .Select(s => s.Replace("\"", ""));
            if (exacts.HasItems()) {
                Querystring["exact"] = exacts.Join(" ");
                var exact = "\"" + exacts.Join(" ") + "\"~";
                var query = new MultiFieldQueryParser(version, props, analyser).Parse(exact);
                BaseQuery.Add(query, BooleanClause.Occur.MUST);
            }

            var mustnot = rootnode.GetParameter("mustnot");
            if (!string.IsNullOrEmpty(mustnot)) {
                mustnot = mustnot.Replace("\"", "").Split(' ').Join(" ");
                var query = new MultiFieldQueryParser(version, props, analyser).Parse(mustnot);
                BaseQuery.Add(query, BooleanClause.Occur.MUST_NOT);
                Querystring["mustnot"] = mustnot;
            }

            foreach (var node in rootnode.ChildNodes) {
                RecursiveFilter(rootnode, node, Metadata);
            }

            DoRangeQueries();

            DoPagesize();
        }


        void DoRangeQueries() {
            foreach (var range in _ranges) {
                if (range.Value.Type == typeof(DateTime)) DoDateTimeRange(range);
                if (range.Value.Type == typeof(int)) DoIntRange(range);
                if (range.Value.Type == typeof(double)) DoDoubleRange(range);
            }
        }

        void DoDateTimeRange(KeyValuePair<string, Range> pair) {
            string lower, upper;
            DateTime dt;
            lower = DateTime.TryParse(pair.Value.Low, out dt)
                    ? dt.ToString("yyyyMMdd")
                    : DateTime.MinValue.ToString("yyyyMMdd");
            upper = DateTime.TryParse(pair.Value.High, out dt)
                    ? dt.ToString("yyyyMMdd")
                    : DateTime.MaxValue.ToString("yyyyMMdd");
            var r = new TermRangeQuery(pair.Key, lower, upper, true, true);
            BaseQuery.Add(r, BooleanClause.Occur.MUST);
        }

        void DoDoubleRange(KeyValuePair<string, Range> pair) {
            var lower = double.MinValue;
            var upper = double.MaxValue;
            double.TryParse(pair.Value.Low, out lower);
            double.TryParse(pair.Value.High, out upper);
            var r = NumericRangeQuery.NewDoubleRange(pair.Key, lower, upper, true, true);
            BaseQuery.Add(r, BooleanClause.Occur.MUST);
        }

        void DoIntRange(KeyValuePair<string, Range> pair) {
            var lower = int.MinValue;
            var upper = int.MaxValue;
            int.TryParse(pair.Value.Low, out lower);
            int.TryParse(pair.Value.High, out upper);
            var r = NumericRangeQuery.NewIntRange(pair.Key, lower, upper, true, true);
            BaseQuery.Add(r, BooleanClause.Occur.MUST);
        }

        void RecursiveFilter(CompositeNode parent, Node node, IClassMetadata metadata) {
            var leafnode = node as LeafNode;
            if (leafnode != null) {
                FilterLeafNode(parent, leafnode, metadata);
                return;
            }
            var compositenode = node as CompositeNode;
            if (compositenode == null) return;
            FilterCompositeNode(compositenode, metadata);
        }

        void FilterCompositeNode(CompositeNode node, IClassMetadata metadata) {
            if (!metadata.PropertyNames.Contains(node.Name)) return;

            var proptype = metadata.GetPropertyType(node.Name);
            if (proptype is EntityType) {
                var childmetadata = AR.Holder.GetClassMetadata(proptype.ReturnedClass);
                foreach (var childNode in node.ChildNodes) {
                    RecursiveFilter(node, childNode, childmetadata);
                }
            } else if (proptype is CollectionType) {
                var ctype = (CollectionType) proptype;
                var reltype = ctype.ReturnedClass.GetGenericArguments().FirstOrDefault();
                if (reltype == null) return;

                var childmetadata = AR.Holder.GetClassMetadata(reltype);
                if (childmetadata == null) return;

                foreach (var childnode in node.ChildNodes)
                    RecursiveFilter(node, childnode, childmetadata);

            }
        }

        void FilterLeafNode(CompositeNode parent, LeafNode leafnode, IClassMetadata metadata) {
            if (string.IsNullOrEmpty(leafnode.Value.ToString())) return;
            if (leafnode.IsArray && string.IsNullOrEmpty(((string[]) leafnode.Value).Join(""))) return;
            if (leafnode.Value.ToString() == "all") return;

            var fieldname = leafnode.FullName.Replace("root.", "");
            if (Fields.Contains(fieldname)) {
                StraightLeafNode(parent, leafnode, fieldname);
            } else {
                RangeLeafNode(leafnode, metadata);
            }
        }

        void StraightLeafNode(CompositeNode parent, LeafNode leafnode, string fieldname) {
            var qp = new QueryParser(LuceneSearchHelper.LuceneVersion, fieldname, _analyser);
            var op = QueryParser.Operator.AND;
            var o = parent.GetParameter(leafnode.Name + "_op");
            if (!string.IsNullOrEmpty(o) && o == "or")
                op = QueryParser.Operator.OR;

            qp.SetDefaultOperator(op);
            string[] val;
            if (leafnode.IsArray) {
                val = (leafnode.Value as string[])
                    .Select(s => HttpUtility.UrlDecode(s).Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToArray();
            } else {
                val = HttpUtility.UrlDecode(leafnode.Value.ToString()).Split(',')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToArray();
            }
            var parse = val.Join(" ");
            if (string.IsNullOrEmpty(parse)) return;

            Query q = qp.Parse(parse);

            var occurence = BooleanClause.Occur.MUST;
            var x = parent.GetParameter(leafnode.Name + "modifier");
            if (!string.IsNullOrEmpty(o)) {
                switch (x) {
                    case "must":
                        occurence = BooleanClause.Occur.MUST;
                        break;
                    case "mustnot":
                        occurence = BooleanClause.Occur.MUST_NOT;
                        break;
                    case "should":
                        occurence = BooleanClause.Occur.SHOULD;
                        break;
                }
            }
            BaseQuery.Add(q, occurence);
            Querystring[fieldname] = val.Join(",");
            Querystring[fieldname + "_op"] = o;
            Querystring[fieldname + "modifier"] = x;
        }

        void RangeLeafNode(LeafNode node, IClassMetadata metadata) {
            var proptree = node.FullName.SplitAndTrim(".").Skip(1).ToList();
            if (proptree.IsEmpty()) return;

            var prop = proptree.LastOrDefault();
            if (prop == null || prop.Length < 3) return;
            var mod = prop.SplitAndTrim("_").LastOrDefault();
            var propfield = prop.Substring(0, prop.Length - 3);
            proptree[proptree.Count - 1] = propfield;
            var field = proptree.Join(".");

            var type = metadata.PropertyNames.Contains(propfield)
                            ? metadata.GetPropertyType(propfield).ReturnedClass
                            : GuessType(node);

            if (type == null) return;

            switch(mod) {
                case "gt":
                case "ge":
                    RegisterRange(field, node.Value.ToString(), null, type);
                    break;
                case "lt":
                case "le":
                    RegisterRange(field, null, node.Value.ToString(), type);
                    break;
            }
            Querystring[node.FullName.SplitAndTrim(".").Skip(1).Join(".")] = node.Value.ToString();
        }

        IDictionary<string, Range> _ranges = new Dictionary<string, Range>();

        void RegisterRange(string field, string low, string high, Type type) {
            if (_ranges.ContainsKey(field)) {
                var r = _ranges[field];
                if (!string.IsNullOrEmpty(low) && !string.IsNullOrEmpty(r.Low)) {
                    r.Low = low;
                }
                if (!string.IsNullOrEmpty(high) && !string.IsNullOrEmpty(r.High)) {
                    r.High = high;
                }
            } else {
                _ranges.Add(field, new Range {Type = type, Low = low, High = high});
            }
        }

        Type GuessType(LeafNode node) {
            DateTime dt;
            if (DateTime.TryParse(node.Value.ToString(), out dt)) return typeof (DateTime);

            int i;
            if (int.TryParse(node.Value.ToString(), out i)) return typeof (int);

            double f;
            if (double.TryParse(node.Value.ToString(), out f)) return typeof (double);

            return null;
        }

        internal struct Range {
            public Type Type { get; set; }
            public string Low { get; set; }
            public string High { get; set; }
        }

        IEnumerable<T> Run(IEnumerable<object> ids) {
            var hql = "from " + Type.FullName + " _this ";
            if (ids.HasItems()) {
                hql += " where _this." + Metadata.IdentifierPropertyName + " in (" + string.Join(",", ids) + ")";
            }
            hql += DoOrderby ? GenerateOrderByString() : string.Empty;

            var hasmanys = Fetch.Where(f => Metadata.PropertyNames.Contains(f) && Metadata.GetPropertyType(f) is CollectionType);

            if (hasmanys.HasItems()) {
                var q = new DetachedQuery("select _this.Id " + hql);
                if (PageSize != 0)
                    q.SetMaxResults(PageSize).SetFirstResult(PageSize * (Page - 1));
                var fetchids = q.List<T, object>();

                if (fetchids.HasItems())
                    foreach (var f in hasmanys) {
                        var fetchhql = "from " + Type.FullName + " _that ";
                        fetchhql += "left join fetch _that." + f;
                        fetchhql += " where _that." + Metadata.IdentifierPropertyName + " in ( " + string.Join(",", fetchids) + " )";
                        new DetachedQuery(fetchhql).Future<T>();
                    }
            }

            return RunItemsWithBelongsFetch(ids);
        }

        public override IEnumerable<T> Run(CompositeNode rootnode) {
            GenerateQuery(rootnode);
            var ids = LuceneSearchHelper.GetIds(Type, BaseQuery).ToList();
            if (ids.IsEmpty() && !string.IsNullOrEmpty(BaseQuery.ToString())) {
                ids.Add(0);
            }
            return Run(ids);
        }

        public override IEnumerable<T> Run(CompositeNode rootnode, ref IFutureValue<long> count) {
            GenerateQuery(rootnode);
            var ids = LuceneSearchHelper.GetIds(Type, BaseQuery).ToList();
            if (ids.IsEmpty() && !string.IsNullOrEmpty(BaseQuery.ToString())) {
                ids.Add(0);
            }

            var hql = "from " + Type.FullName + " _this ";
            if (ids.HasItems()) {
                hql += " where _this." + Metadata.IdentifierPropertyName + " in (" + string.Join(",", ids) + ")";
            }
            count = new DetachedQuery("select count(*) " + hql).FutureValue<T, long>();
            return Run(ids);
        }

        IEnumerable<T> RunItemsWithBelongsFetch(IEnumerable<object> ids) {
            var belongs = Fetch.Where(f => Metadata.PropertyNames.Contains(f) && Metadata.GetPropertyType(f) is EntityType);
            var hql = "from " + Type.FullName + " _this ";
            hql += belongs.Select(f => " left join fetch _this." + f).Join(" ");
            if (ids.HasItems()) {
                hql += " where _this." + Metadata.IdentifierPropertyName + " in (" + string.Join(",", ids) + ")";
            }
            hql += GenerateOrderByString();
            var q = new DetachedQuery(hql);
            if (PageSize != 0)
                q.SetMaxResults(PageSize).SetFirstResult(PageSize * (Page - 1));
            return q.Future<T>();
        }

        string GenerateOrderByString() {
            var orders = new List<string>();
            var orderby = RootNode.GetParameter("orderby");
            if (!string.IsNullOrEmpty(orderby)) {
                if (orderby == "none") return string.Empty;

                var o = orderby.Split('.');
                if ((o.Length == 2 && Metadata.PropertyNames.Contains(o[0]) && Metadata.GetPropertyType(o[0]) is EntityType) || (o.Length == 1 && Metadata.PropertyNames.Contains(orderby))) {
                    var order = " _this." + orderby;
                    order += RootNode.GetParameter("desc") == null ? " asc" : " desc";
                    orders.Add(order);
                }
                Querystring["orderby"] = orderby;
                if (RootNode.GetParameter("desc") != null) {
                    Querystring["desc"] = null;
                }
            } else {
                orders.AddRange(
                    from attr in base.OrderBy.OrderBy(a => a.Order)
                    let order = " _this." + attr.Prop
                    select order + (attr.Ascending ? " asc" : " desc")
                );
            }

            return orders.HasItems() ? " order by " + string.Join(" , ", orders) : string.Empty;
        }
    }
}
