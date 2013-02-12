#region using

using System.IO;
using System.Linq;
using Castle.MonoRail.Framework;
using Dry.Common.Model;

#endregion

namespace Dry.Common.Monorail.Filters {
    public class WhitespaceFilter : IFilter {
        public static readonly string[] Compresstypes = new[] {"text/xhtml", "text/html", "text/xml"};


        public bool Perform(ExecuteWhen exec, IEngineContext context, IController controller, IControllerContext controllerContext) {
            if (Settings.Get("build").Equals("release")) {
                var c = context.Response.ContentType;
                if (Compresstypes.Contains(c))
                    context.UnderlyingContext.Response.Filter = context.Services.TransformFilterFactory.Create(typeof (WhitespaceTransformFilter), context.UnderlyingContext.Response.Filter) as Stream;
            }

            return true;
        }
    }
}
