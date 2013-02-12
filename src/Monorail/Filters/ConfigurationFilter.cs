#region using

using System.Configuration;
using Castle.MonoRail.Framework;
using Dry.Common.Model;

#endregion

namespace Dry.Common.Monorail.Filters {
    public class ConfigurationFilter : IFilter {
        public bool Perform(ExecuteWhen exec, IEngineContext context, IController controller,
                            IControllerContext controllerContext) {
            foreach (var key in ConfigurationManager.AppSettings.AllKeys) {
                controllerContext.PropertyBag[key] = ConfigurationManager.AppSettings[key];
            }
            foreach (var s in Settings.AllKeys) {
                controllerContext.PropertyBag[s] = Settings.Get(s);
            }
            return true;
        }
    }
}
