#region using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.Components.Binder;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Metadata;
using NHibernate.Persister.Collection;
using NHibernate.SqlCommand;
using NHibernate.Transform;
using NHibernate.Type;

#endregion

namespace Dry.Common.Queries {
    public class GenericNhQuery<T> : AbstactQuery<T> where T : class {
        public DetachedCriteria Query { get; set; }
        public IClassMetadata Metadata { get; private set; }
        public ISessionFactory SessionFactory { get; private set; }

        public GenericNhQuery() {
            SessionFactory = AR.Holder.GetSessionFactory(Type);
            Metadata = SessionFactory.GetClassMetadata(Type);
        }

        IEnumerable<T> Run() {
            DoPropertyFetchQueries();
            if (PageSize != 0)
                Query.SetFirstResult((Page - 1) * PageSize).SetMaxResults(PageSize);
            return Query.Future<T>();
        }

        public override IEnumerable<T> Run(CompositeNode rootnode) {
            GenerateQuery(rootnode);
            return Run();
        }

        public override IEnumerable<T> Run(CompositeNode rootnode, ref IFutureValue<long> count) {
            GenerateQuery(rootnode);
            count = Count();
            return Run();
        }

        protected virtual IFutureValue<long> Count() {
            var dc = CriteriaTransformer.Clone(Query);
            dc.ClearOrders();
            return dc.SetProjection(Projections.RowCountInt64()).FutureValue<T, long>();
        }

        protected virtual void DoPropertyFetchQueries() {
            if (Fetch == null || !Fetch.HasItems()) return;

            var subquery = CriteriaTransformer.Clone(Query);
            subquery.SetProjection(Projections.Id());
            if (PageSize != 0)
                subquery.SetFirstResult((Page - 1) * PageSize).SetMaxResults(PageSize);

            foreach (var f in Fetch) {
                DetachedCriteria.For<T>().SetCacheable(true)
                    .Add(Subqueries.PropertyIn(Metadata.IdentifierPropertyName, subquery ))
                    .SetFetchMode(f.Trim(), FetchMode.Join).Future<T>();
            }
        }

        public GenericNhQuery<T> GenerateQuery(CompositeNode rootnode) {
            RootNode = rootnode;
            Query = DetachedCriteria.For(Type).SetResultTransformer(Transformers.DistinctRootEntity);
            DoTextSearch();

            foreach (var node in rootnode.ChildNodes) {
                RecursiveFilter(Query, node, Metadata);
            }

            SetupOrderBy();    
            DoPagesize();

            return this;
        }

        public IQuery<T> SetupOrderBy() {
            if (_hasLuceneSearch) return this;

            var orderby = RootNode.GetParameter("orderby");
            if (!string.IsNullOrEmpty(orderby)) {
                var orders = orderby.SplitAndTrim(".").ToArray();
                if (orders.Length == 2 && Metadata.PropertyNames.Contains(orders[0])) {
                    if (_childCriterias.ContainsKey("root." + orders[0])) {
                        _childCriterias["root." + orders[0]].AddOrder(new Order(orders[1], RootNode.GetParameter("desc") == null));
                    } else {
                        Query.CreateCriteria(orders[0], Type.Name.ToLower() + "order", JoinType.LeftOuterJoin)
                            .AddOrder(new Order(orders[1], RootNode.GetParameter("desc") == null));
                    }
                } else if (orders.Length == 1 && Metadata.PropertyNames.Contains(orderby)) {
                    Query.AddOrder(new Order(orderby, RootNode.GetParameter("desc") == null));
                }
                Querystring["orderby"] = orderby;
                if (RootNode.GetParameter("desc") != null) {
                    Querystring["desc"] = null;
                }
            } else {
                foreach (var attr in OrderBy.OrderBy(a => a.Order)) {
                    var prop = attr.Prop.Split('.');
                    if (prop.Length == 2 && Metadata.PropertyNames.Contains(prop[0])) {
                        _childCriterias.TryGet("root." + prop[0], () => Query.CreateCriteria(prop[0], JoinType.LeftOuterJoin)).AddOrder(new Order(prop[1], attr.Ascending));
                    } else {
                        Query.AddOrder(new Order(attr.Prop, attr.Ascending));
                    }
                }
            }
            return this;
        }

        void RecursiveFilter(DetachedCriteria query, Node node, IClassMetadata metadata) {
            var leafnode = node as LeafNode;
            var compositenode = node as CompositeNode;
            if (leafnode != null) {
                FilterLeafNode(leafnode, metadata, query);
                return;
            }
            if (compositenode != null) {
                FilterCompositeNode(compositenode, metadata, query);
            }
        }

        readonly IDictionary<string, DetachedCriteria> _childCriterias = new Dictionary<string, DetachedCriteria>();

        void FilterCompositeNode(CompositeNode compositenode, IClassMetadata metadata, DetachedCriteria query) {
            if (!metadata.PropertyNames.Contains(compositenode.Name)) return;
            var proptype = metadata.GetPropertyType(compositenode.Name);
            if (proptype is EntityType) {
                var etype = (EntityType) proptype;
                var childmetadata = AR.Holder.GetClassMetadata(etype.ReturnedClass);
                var childcriteria = _childCriterias.TryGet(compositenode.Name, () => query.CreateCriteria(compositenode.Name, JoinType.LeftOuterJoin));
                foreach (var childnode in compositenode.ChildNodes) {
                    RecursiveFilter(childcriteria, childnode, childmetadata);
                }
            } else if (proptype is CollectionType) {
                var ctype = (CollectionType) proptype;
                var reltype = ctype.ReturnedClass.GetGenericArguments().FirstOrDefault();
                var childmetadata = AR.Holder.GetClassMetadata(reltype);
                if (childmetadata == null) return;

                var persister = SessionFactory.GetCollectionMetadata(ctype.Role) as ICollectionPersister;
                if (persister == null) return;

                if (persister.IsOneToMany) {
                    
                } else if (persister.IsManyToMany) {
                    
                } 
            }
        }

        void FilterLeafNode(LeafNode leafnode, IClassMetadata metadata, DetachedCriteria query) {
            if (leafnode.Name == metadata.IdentifierPropertyName) {
                FilterPrimaryKey(leafnode, metadata, query);
                return;
            }

            var propsplit = leafnode.Name.Split('_');
            var prop = propsplit.FirstOrDefault();
            var mode = propsplit.LastOrDefault();

            if (prop == null) return;
            if (!metadata.PropertyNames.Contains(prop)) return;

            var proptype = metadata.GetPropertyType(prop);
            if (proptype is EntityType) {
                var etype = (EntityType) proptype;
                if (etype.IsNullable) {
                    switch (leafnode.Value.ToString()) {
                        case "null":
                            query.Add(Restrictions.IsNull(prop));
                            SetParam(leafnode);
                            return;
                        case "notnull":
                            query.Add(Restrictions.IsNotNull(prop));
                            SetParam(leafnode);
                            return;
                    }
                }
            } else {
                if (leafnode.Value.ToString() == "all") return;
                bool isconverted = FilterProperty(leafnode, proptype.ReturnedClass, query, prop, mode);
                if (isconverted) return;
                Logger.Warn(string.Format("Could not convert value '{0}' to type '{1}'", leafnode.Value, proptype.ReturnedClass));
            }
        }

        bool FilterProperty(LeafNode leafnode, Type proptype, DetachedCriteria query, string prop, string mode) {
            var converter = new DefaultConverter();
            var isconverted = false;
            var val = converter.Convert(proptype, leafnode.Value, out isconverted);

            if (prop == mode) {
                if (isconverted) {
                    query.Add(Restrictions.Eq(prop, val));
                    SetParam(leafnode);
                }
            } else {
                if (isconverted) {
                    SetParam(leafnode);
                    switch (mode.ToLower()) {
                        case "gt":
                            query.Add(Restrictions.Gt(prop, val));
                            break;
                        case "ge":
                            query.Add(Restrictions.Ge(prop, val));
                            break;
                        case "lt":
                            query.Add(Restrictions.Le(prop, val));
                            break;
                        case "le":
                            query.Add(Restrictions.Le(prop, val));
                            break;
                        case "like":
                            query.Add(Restrictions.InsensitiveLike(prop, val.ToString(), MatchMode.Anywhere));
                            break;
                        case "startswith":
                            query.Add(Restrictions.InsensitiveLike(prop, val.ToString(), MatchMode.Start));
                            break;
                    }
                }
            }
            return isconverted; 
        }

        void FilterPrimaryKey(LeafNode leafnode, IClassMetadata metadata, DetachedCriteria query) {
            if (string.IsNullOrEmpty(leafnode.Value.ToString())) return;
            if (leafnode.Value.Equals("all")) return;

            var valuesplit = HttpUtility.UrlDecode(leafnode.Value.ToString()).Split(',');
            var pktype = metadata.IdentifierType.ReturnedClass;
            if (valuesplit.Length > 1) {
                try {
                    var ar = valuesplit
                        .Select(p => {
                            var success = false;
                            var id = Converter.Convert(pktype, typeof (string), p, out success);
                            return success ? id : null;
                        })
                        .Where(p => p != null)
                        .Select(p => Restrictions.Eq(Projections.Id(), p)).ToArray();

                    if (ar.Length > 0) {
                        var disjunction = Restrictions.Disjunction();
                        foreach (var o in ar) {
                            disjunction.Add(o);
                        }
                        query.Add(disjunction);
                        SetParam(leafnode);
                    }
                } catch (Exception e) {
                    Logger.ErrorFormat(e, "Could not convert value '{0}' to type '{2}'", leafnode.Value, metadata.IdentifierType.ReturnedClass);
                }
            } else {
                if (leafnode.Value.Equals("null")) {
                        query.Add(Restrictions.IsNull(Projections.Id()));
                        SetParam(leafnode);
                } else {
                    var success = false;
                    Converter.CanConvert(pktype, typeof (string), leafnode.Value.ToString(), out success);
                    if (success) {
                        var id = Converter.Convert(pktype, typeof(string), leafnode.Value.ToString(), out success);
                        query.Add(Restrictions.Eq(Projections.Id(), id));
                        SetParam(leafnode);
                    }
                }
            }
        }

        void SetParam(LeafNode node) {
            Querystring[node.FullName.Replace("root.", "")] = node.Value;
        }

        public void DoTextSearch() {
            var term = RootNode.GetParameter("term");
            if (string.IsNullOrEmpty(term)) return;
/*
            _hasLuceneSearch = false;
            if (typeof (T).HasAttribute<IndexedAttribute>()) {
                DoLuceneSearch(term);
                _hasLuceneSearch = true;
            }
            else {
                DoLikeSearch(term);
            }
 */
            DoLikeSearch(term);
        }

        void DoLikeSearch(string term) {
            var q = term.Trim();
            if (!string.IsNullOrEmpty(q)) {
                q = q.Trim();
                var disjunction = Restrictions.Disjunction();
                Querystring["term"] = q;
                foreach (var prop in Metadata.PropertyNames) {
                    var proptype = Metadata.GetPropertyType(prop);
                    if (proptype is NHibernate.Type.StringClobType)
                        return;
                    else if (proptype is ComponentType) {
                        // do component props
                    } else if (proptype is EntityType) {
                        var subtype = AR.Holder.GetClassMetadata(proptype.ReturnedClass);
                        var subquery = DetachedCriteria.For(proptype.ReturnedClass)
                            .SetProjection(Projections.Id());
                        var subdisjunction = Restrictions.Disjunction();
                        foreach (var subprop in subtype.PropertyNames) {
                            var subproptype = subtype.GetPropertyType(subprop);
                            if (subproptype is StringType) {
                                subdisjunction.Add(Restrictions.InsensitiveLike(subprop, q, MatchMode.Anywhere));
                            }
                        }
                        disjunction.Add(Subqueries.PropertyIn(prop, subquery.Add(subdisjunction)));
                    } else if (proptype is StringType) {
                        disjunction.Add(Restrictions.InsensitiveLike(prop, q, MatchMode.Anywhere));
                    }
                }
                Query.Add(disjunction);
            }
        }
    }
}