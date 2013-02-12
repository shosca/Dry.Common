#region using

using Castle.MonoRail.Framework;

#endregion

namespace Dry.Common.Monorail.Filters {
    public class TracingFilter : IFilter {
        public bool Perform(ExecuteWhen exec, IEngineContext context, IController controller,
                            IControllerContext controllerContext) {
            context.Trace.Write(exec.ToString());
            return true;
        }
    }
}
