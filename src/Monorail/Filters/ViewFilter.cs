#region using

using System.Globalization;
using System.IO;
using Castle.MonoRail.Framework;
using Dry.Common.Monorail.Helpers;

#endregion

namespace Dry.Common.Monorail.Filters {
    public class ViewFilter : IFilter {
        public bool Perform(ExecuteWhen exec, IEngineContext context, IController controller, IControllerContext controllerContext) {
            if (controllerContext.SelectedViewName == null) return true;

            if (context.IsAjax()) {
                controllerContext.LayoutNames = null;
            }

            var p = Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture);
            var template = new TemplateLocation();

            if (context.Request.AcceptHeader != null && context.Request.AcceptHeader.Contains("application/json")) {
                template.Area = controllerContext.AreaName;
                template.Controller = controllerContext.Name;
                template.Action = "json";
                controllerContext.LayoutNames = null;
            } else {
                var view = context.GetParameter("view");

                template.Area = controllerContext.AreaName;
                template.Controller = controllerContext.Name;
                template.Action = string.IsNullOrEmpty(view) ? controllerContext.Action : view;

                if (view == "json") {
                    controllerContext.LayoutNames = null;
                } else {
                    var layout = context.GetParameter("layout");
                    if (context.Services.ViewEngineManager.HasTemplate(p + "layouts" + p + layout)) {
                        controllerContext.LayoutNames = new[] {layout};
                    }
                }
            }

            var t = template.ToString();
            if (context.Services.ViewEngineManager.HasTemplate(t)) {
                controllerContext.SelectedViewName = t;
            } else {
                template.Area = null;
                template.Controller = "common";
                t = template.ToString();
                if (context.Services.ViewEngineManager.HasTemplate(t)) {
                    controllerContext.SelectedViewName = t;
                }
            }
            return true;
        }
    }
}
