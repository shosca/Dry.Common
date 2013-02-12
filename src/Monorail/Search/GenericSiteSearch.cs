#region using

using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Helpers;
using Dry.Common.Monorail.DynamicActions;
using Dry.Common.Queries;
using NHibernate;

#endregion

namespace Dry.Common.Monorail.Search {
    public abstract class GenericSiteSearch<T> : ISiteSearch where T : class {
        public IQuery<T> Query { get; private set; }

        protected GenericSiteSearch(IQuery<T> nhQuery) {
            Query = nhQuery;
        }

        public virtual string Name {
            get { return typeof (T).Name; }
        }

        public Type Type {
            get { return typeof (T); }
        }

        public virtual string ViewComponent {
            get { return Type.Name + "Search"; }
        }

        public bool HasResults {
            get { return Items.HasItems(); }
        }

        public DictHelper.MonoRailDictionary Querystring {
            get { return Query.Querystring; }
        }

        public IEnumerable<T> Items { get; private set; }

        public virtual void Search(IEngineContext context) {
            Query
                .AddOrderBy(
                    GetType().GetAttrs<QueryOrderByAttribute>().Select(qo => qo.OrderBy)
                )
                .AddQueryFilter(
                    GetType().GetAttrs<QueryFilterAttribute>().Select(qa =>
                    {
                        var contextaware = qa as IContextAware;
                        if (contextaware != null) contextaware.SetContext(context);
                        return qa.QueryFilter;
                    })
                )
                .SetupPaging(25)
                .AddEager(GetType().GetAttrs<FetchAttribute>().SelectMany(a => a.FetchProperties));

            IFutureValue<long> count = null;
            Items = new LazyPage<T>(Query.Run(context.Request.QueryStringNode, ref count), Query.Page, Query.PageSize, count);
        }

        protected IEnumerable<QueryFilter> GetQueryFiltersFrom(IEngineContext context, Type type) {
            return type.GetAttrs<QueryFilterAttribute>().Select(qf => {
                var contextaware = qf as IContextAware;
                if (contextaware != null)
                    contextaware.SetContext(context);
                return qf.QueryFilter;
            });
        }

        protected IEnumerable<OrderBy> GetOrderBysFrom(Type type) {
            return type.GetAttrs<QueryOrderByAttribute>().Select(qo => qo.OrderBy);
        }
    }
}
