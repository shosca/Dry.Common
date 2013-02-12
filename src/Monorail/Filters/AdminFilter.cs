#region using

using Castle.MonoRail.Framework;
using Dry.Common.Monorail.Helpers;

#endregion

namespace Dry.Common.Monorail.Filters {
    public class AdminFilter : IFilter {
        public bool Perform(ExecuteWhen exec, IEngineContext context, IController controller, IControllerContext controllerContext) {
            return context.CurrentUser.IsInRole("Admin") || context.Handle403();
        }
    }
}
