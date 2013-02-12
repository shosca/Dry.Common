#region using

using System.Web;
using Castle.MonoRail.Framework.Routing;
using Castle.Windsor;

#endregion

namespace Dry.Common.Monorail {
    public class WebApplication : HttpApplication, IContainerAccessor {
        public IWindsorContainer Container {
            get {
                return IoC.Container;
            }
        }

        public void Application_OnEnd() {
            IoC.Dispose();
        }

        public void Application_OnStart() {
            IoC.Initialize();
            Routing.SetupRoutes(RoutingModuleEx.Engine);
        }
    }
}
