#region using

using System;
using System.Collections.Generic;
using Castle.Components.Binder;
using Castle.Core.Logging;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Helpers;
using Dry.Common.Monorail.DynamicActions;
using Dry.Common.Monorail.Helpers;
using NHibernate;

#endregion

namespace Dry.Common.Queries {
    public abstract class AbstactQuery<T> : IQuery<T> {
        ILogger _logger = NullLogger.Instance;
        public ILogger Logger {
            get { return _logger; }
            set { _logger = value; }
        }

        protected bool _hasLuceneSearch = false;
        protected CompositeNode RootNode { get; set; }
        protected IConverter Converter = new Castle.Components.Binder.DefaultConverter();

        readonly ISet<QueryFilter> _defaultFilters = new HashSet<QueryFilter>();
        public ISet<QueryFilter> DefaultFilters {
            get { return _defaultFilters; }
        }

        readonly ISet<OrderBy> _orderbys = new HashSet<OrderBy>();
        public ISet<OrderBy> OrderBy {
            get { return _orderbys; }
        }

        public IQuery<T> AddQueryFilter(IEnumerable<QueryFilter> qfs) {
            DefaultFilters.AddRange(qfs);
            return this;
        }

        public IQuery<T> AddQueryFilter(params QueryFilter[] qf) {
            DefaultFilters.AddRange(qf);
            return this;
        }

        public IQuery<T> AddOrderBy(IEnumerable<OrderBy> orderbys) {
            OrderBy.AddRange(orderbys);
            return this;
        }

        public IQuery<T> AddOrderBy(params OrderBy[] o) {
            OrderBy.AddRange(o);
            return this;
        }

        public Type Type { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        public DictHelper.MonoRailDictionary Querystring { get; protected set; }

        readonly ISet<string> _fetch = new HashSet<string>();
        protected ISet<string> Fetch {
            get { return _fetch; }
        }

        public abstract IEnumerable<T> Run(CompositeNode rootnode);
        public abstract IEnumerable<T> Run(CompositeNode rootnode, ref IFutureValue<long> count);

        protected void DoPagesize() {
            Page = GetCurrentPageFromRequest(RootNode);
            var ps = RootNode.GetParameter("pageSize");
            var psize = PageSize;
            if (!string.IsNullOrEmpty(ps) && int.TryParse(ps, out psize)) {}
            PageSize = psize;
            Querystring["pageSize"] = PageSize;
        }

        protected AbstactQuery() {
            Querystring = new DictHelper.MonoRailDictionary();
            PageSize = MRHelper.DefaultPageSize;
            Page = 1;
            Type = typeof (T);
        }

        public void SetContext(IEngineContext context) {
        }

        public virtual IQuery<T> SetupPaging() {
            return SetupPaging(MRHelper.DefaultPageSize);
        }

        public virtual IQuery<T> SetupPaging(int pagesize) {
            PageSize = pagesize;
            return this;
        }

        public virtual IQuery<T> AddEager(IEnumerable<string> fetches) {
            Fetch.AddRange(fetches);
            return this;
        }

        public virtual IQuery<T> AddEager(params string[] fetches) {
            Fetch.AddRange(fetches);
            return this;
        }

        public static int GetCurrentPageFromRequest(CompositeNode rootnode) {
            var currentPage = rootnode.GetParameter("page");
            var curPage = 1;
            if (!string.IsNullOrEmpty(currentPage) && int.TryParse(currentPage, out curPage)) {}
            return curPage <= 0 ? 1 : curPage;
        }
    }
}
