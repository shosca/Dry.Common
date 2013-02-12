#region using

using System;
using Castle.MonoRail.Framework;
using Dry.Common.Monorail.Helpers;
using Dry.Common.Queries;
using NHibernate;

#endregion

namespace Dry.Common.Monorail.DynamicActions {
    public class ListAction<T, TK> : BaseAction<T> where T : class where TK : IQuery<T> {
        public IQuery<T> Query { get; private set; }

        public override string Action { get { return "index"; } }

        public ListAction(TK nhQuery) {
            Query = nhQuery;
        }

        public override object Execute(IEngineContext context, IController controller, IControllerContext controllerContext) {
            base.Execute(context, controller, controllerContext);
            var method = context.GetParameter("_method") ?? context.Request.HttpMethod;
            switch (method.ToUpper()) {
                case "GET":
                    return ExecuteList(context, controller, controllerContext);
                case "POST":
                    controllerContext.Action = "create";
                    return ExecuteCreate(context, controller, controllerContext);
                default:
                    throw new MonoRailException("Unsupported method.");
            }
        }

        public object ExecuteList(IEngineContext context, IController controller, IControllerContext controllerContext) {
            OnPreList(controller);
            try {

                var source = context.Request.HttpMethod == "GET" ? context.Request.QueryString : context.Request.Form;
                var store = BuildCompositeNode(source, Queryfilters, false);

                IFutureValue<long> count = null;

                Query
                    .AddOrderBy(Orderbys)
                    .AddQueryFilter(Queryfilters)
                    .SetupPaging(PageSize)
                    .AddEager(Fetches);

                var items = Query.Run(store, ref count);

                if (Query.PageSize == 0)
                    controllerContext.PropertyBag["items"] = new CustomPage<T>(items, Query.Page, Convert.ToInt32(count.Value), Convert.ToInt32(count.Value));
                else
                    controllerContext.PropertyBag["items"] = new LazyPage<T>(items, Query.Page, Query.PageSize, count);

                OnPostList(controller, items);

            } catch (Exception ex) {
                context.HandleError(500, ex);
            } finally {
                controllerContext.PropertyBag["qs"] = Query.Querystring;
                foreach (var key in Query.Querystring.Keys) {
                    controllerContext.PropertyBag[key] = Query.Querystring[key];
                }
            }
            return null;
        }
    }
}
