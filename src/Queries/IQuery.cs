#region using

using System;
using System.Collections.Generic;
using Castle.Components.Binder;
using Castle.MonoRail.Framework.Helpers;
using Dry.Common.Monorail.DynamicActions;
using NHibernate;

#endregion

namespace Dry.Common.Queries {
    public interface IQuery<out T> {
        Type Type { get; set; }
        int Page { get; }
        int PageSize { get; }
        DictHelper.MonoRailDictionary Querystring { get; }
        ISet<QueryFilter> DefaultFilters { get; }
        ISet<OrderBy> OrderBy { get; }

        IQuery<T> AddQueryFilter(params QueryFilter[] qf);
        IQuery<T> AddQueryFilter(IEnumerable<QueryFilter> qfs);
        IQuery<T> AddOrderBy(params OrderBy[] order);
        IQuery<T> AddOrderBy(IEnumerable<OrderBy> orders);
        IQuery<T> AddEager(params string[] fetches);
        IQuery<T> AddEager(IEnumerable<string> fetches);

        IQuery<T> SetupPaging(int pagesize);
        IEnumerable<T> Run(CompositeNode rootnode);
        IEnumerable<T> Run(CompositeNode rootnode, ref IFutureValue<long> count);
    }
}
