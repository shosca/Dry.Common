#region using

using Castle.MonoRail.Framework;
using Dry.Common.Monorail.Helpers;

#endregion

namespace Dry.Common.Monorail.DynamicActions {
    public class ViewAction<T> : BaseAction<T> where T : class {
        public override string Action { get { return "view"; } }

        public override object Execute(IEngineContext context, IController controller, IControllerContext controllerContext) {
            base.Execute(context, controller, controllerContext);
            var method = context.GetParameter("_method") ?? context.Request.HttpMethod;

            switch (method.ToUpper()) {
                case "GET":
                    controllerContext.Action = "view";
                    return ExecuteView(context, controller, controllerContext);
                case "PUT":
                case "POST":
                    controllerContext.Action = "update";
                    return ExecuteUpdate(context, controller, controllerContext);
                case "DELETE":
                    controllerContext.Action = "delete";
                    return ExecuteDelete(context, controller, controllerContext);
                default:
                    throw new MonoRailException("Unsupported method.");
            }
        }
    }
}
