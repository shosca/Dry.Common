#region using

using Castle.MonoRail.Framework;

#endregion

namespace Dry.Common.Monorail.DynamicActions {
    public class UpdateAction<T> : BaseAction<T> where T : class {
        public override string Action { get { return "update"; } }

        public override object Execute(IEngineContext context, IController controller, IControllerContext controllerContext) {
            base.Execute(context, controller, controllerContext);
            switch (context.Request.HttpMethod) {
                case "PUT":
                case "POST":
                    return ExecuteUpdate(context, controller, controllerContext);
                default:
                    throw new MonoRailException("Unsupported method.");
            }
        }
    }
}
