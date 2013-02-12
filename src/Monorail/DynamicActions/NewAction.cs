#region using

using Castle.MonoRail.Framework;

#endregion

namespace Dry.Common.Monorail.DynamicActions {
    public class NewAction<T> : BaseAction<T> where T : class {
        public override string Action { get { return "new"; } }

        public override object Execute(IEngineContext context, IController controller, IControllerContext controllerContext) {
            base.Execute(context, controller, controllerContext);
            return ExecuteNew(context, controller, controllerContext);
        }
    }
}
