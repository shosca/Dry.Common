#region using

using System.IO.Compression;
using System.Web;
using Castle.MonoRail.Framework;

#endregion

namespace Dry.Common.Monorail.Filters {
    public class GZipFilter : IFilter {
        public bool Perform(ExecuteWhen exec, IEngineContext context, IController controller,
                            IControllerContext controllerContext) {
            var acceptEncoding = HttpContext.Current.Request.Headers["Accept-Encoding"];
            if (string.IsNullOrEmpty(acceptEncoding)) return true;

            if (acceptEncoding.Contains("gzip")) {
                context.Response.AppendHeader("Content-Encoding", "gzip");
                context.UnderlyingContext.Response.Filter = new GZipStream(context.UnderlyingContext.Response.Filter,
                                                                           CompressionMode.Compress);
            }
            else if (acceptEncoding.Contains("deflate")) {
                context.Response.AppendHeader("Content-Encoding", "deflate");
                context.UnderlyingContext.Response.Filter = new DeflateStream(context.UnderlyingContext.Response.Filter,
                                                                              CompressionMode.Compress);
            }
            return true;
        }
    }
}
