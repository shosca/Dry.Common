#region using

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using Castle.Core.Logging;
using Dry.Common;
using Dry.Common.Monorail.Helpers;

#endregion

namespace Dry.Common.Handlers {
    public class StaticFileHandler : BaseHttpHandler {
        protected override Task ProcessRequestAsync(HttpContext context) {
            var task = new Task(() => {
                try {
                    var file = context.Server.MapPath(context.Request.Url.LocalPath);
                    if (!File.Exists(file)) return;
                    string etag;
                    using (var fs = new FileStream(file, FileMode.Open)) {
                        etag = new MD5CryptoServiceProvider().ComputeHash(fs).ToBase64String();
                    }
                    var ifnonematch = context.Request.Headers[Header.IfNoneMatch];
                    if (!string.IsNullOrEmpty(ifnonematch) && etag.Equals(ifnonematch)) {
                        context.Response.StatusCode = 304;
                    } else {
                        context.Response.AddHeader(Header.ETag, etag);
                        context.Response.AddHeader(Header.CacheControl, "max-age=630000, public");
                        context.Response.AddHeader(Header.Vary, Header.AcceptEncoding);
                        context.Response.AppendHeader(Header.Expires, DateTime.Now.Date.AddDays(8).ToString("R"));
                        context.Response.ContentType = MimeMapping.GetMimeMapping(file);
                        if (context.Response.ContentType.StartsWith("text/") ||
                            context.Response.ContentType.Equals("application/javascript") ||
                            context.Response.ContentType.Equals("application/json"))
                            context.SetCompression();

                        context.Response.WriteFile(file);
                    }
                } catch (Exception e) {
                    IoC.Resolve<ILogger>().Error("Error in static file handler.", e);
                }
            });
            task.Start();
            return task;
        }
    }
}
