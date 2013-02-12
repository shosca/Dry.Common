#region using

using System;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.Core.Logging;
using Dry.Common.Model;

#endregion

namespace Dry.Common {
    public class LoggingModule : IHttpModule {
        public const string Key = "actiontimetracker";

        public void Init(HttpApplication app) {
            app.BeginRequest += OnBeginRequest;
            app.EndRequest += OnEndRequest;
        }
        private static void OnBeginRequest(object sender, EventArgs e) {
            HttpContext.Current.Items[Key] = DateTime.UtcNow;
        }

        private static void OnEndRequest(object sender, EventArgs e) {
            try {
                var app = sender as HttpApplication;
                var c = app != null ? app.Context : HttpContext.Current;
                if (c == null) return;

                if (c.Request.UserHostAddress.StartsWith("127.0.0.1") || c.Request.UserHostAddress.StartsWith("::")) return;

                var url = c.Request.RawUrl.Contains('?')
                              ? c.Request.RawUrl.Substring(0, c.Request.RawUrl.IndexOf('?'))
                              : c.Request.RawUrl;
                var started = c.Items[Key] as DateTime?;
                if (started == null) return;

                AR.ExecuteStateless<HitLog>(s =>
                    s.Insert(
                        new HitLog {
                            Target = url,
                            Method = c.Request.HttpMethod,
                            ClientHost = c.Request.UserHostAddress,
                            Username = c.User != null ? c.User.Identity.Name : string.Empty,
                            ServiceStatus = c.Response.StatusCode,
                            LogTime = DateTime.Now,
                            UserAgent = c.Request.UserAgent,
                            Parameters = c.Request.HttpMethod.ToLower() == "get" ? c.Request.QueryString.ToString() : c.Request.Form.ToString(),
                            ResponseTime = DateTime.UtcNow - started.Value
                        }
                    )
                );
            } catch (Exception ex) {
                IoC.Resolve<ILogger>().Error("Error while logging request", ex);
            }
        }

        public void Dispose() { }
    }
}
