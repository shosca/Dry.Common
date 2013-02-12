#region using

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Mail;
using System.Web;
using Castle.Core.Logging;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Helpers;
using Castle.MonoRail.Framework.Routing;

#endregion

namespace Dry.Common.Monorail.Helpers {
    public class Header {
        public const string AcceptEncoding = "Accept-Encoding";
        public const string IfNoneMatch = "If-None-Match";
        public const string ContentEncoding = "Content-encoding";
        public const string ETag = "ETag";
        public const string CacheControl = "Cache-Control";
        public const string Expires = "Expires";
        public const string RequestedWith = "X-Requested-With";
        public const string ContentDisposition = "Content-Disposition";
        public const string ContentLength = "Content-Length";
        public const string Vary = "Vary";
    }

    public static class MRHelper {
        public const int DefaultPageSize = 50;

        public static bool IsAjax(this IEngineContext context) {
            if (!string.IsNullOrEmpty(context.Request.Headers[Header.RequestedWith]) &&
                context.Request.Headers[Header.RequestedWith].Equals("XMLHttpRequest")) {
                return true;
            }
            return false;
        }

        public static void SetCompression(this HttpContext context) {
            var accept = context.Request.Headers[Header.AcceptEncoding];
            if (!string.IsNullOrEmpty(accept)) {
                accept = accept.ToUpperInvariant();
                if (accept.Contains("GZIP")) {
                    context.Response.AppendHeader(Header.ContentEncoding, "gzip");
                    context.Response.Filter = new GZipStream(context.Response.Filter, CompressionMode.Compress);
                } else if (accept.Contains("DEFLATE")) {
                    context.Response.AppendHeader(Header.ContentEncoding, "deflate");
                    context.Response.Filter = new DeflateStream(context.Response.Filter, CompressionMode.Compress);
                }
            }
        }

        public static void ErrorMessages(this IEngineContext context, int statuscode, params string[] messages) {
            context.Response.StatusCode = statuscode;
            if (context.Flash["errormessages"] != null) {
                var errormessages = context.Flash["errormessages"] as List<string>;
                if (errormessages == null) {
                    errormessages = new List<string>();
                    context.Flash["errormessages"] = errormessages;
                }
                errormessages.AddRange(messages);
            }
            else {
                var errormessages = new List<string>(messages);
                context.Flash["errormessages"] = errormessages;
            }
        }

        public static ILogger GetLogger(this IEngineContext context) {
            return context.GetService(typeof (ILogger)) as ILogger ?? NullLogger.Instance;
        }

        public static string GetParameter(this IEngineContext context, string parameterName) {
            var value = GetRouteParameter(context, parameterName);
            return value ?? context.Request.Params[parameterName];
        }

        public static string GetRouteParameter(this IEngineContext context, string param) {
            string value = null;
            if (context != null && context.Items.Contains(RouteMatch.RouteMatchKey)) {
                var match = context.Items[RouteMatch.RouteMatchKey] as RouteMatch;

                if (match != null && match.Parameters.ContainsKey(param))
                    value = match.Parameters[param];
            }
            return value;
        }

        public static void SuccessMessage(this IEngineContext context, string message) {
            context.Flash["successmessage"] = message;
        }

        public static void ErrorMessages(this IEngineContext context, params string[] messages) {
            ErrorMessages(context, 500, messages);
        }

        public static void ErrorMessages(this IEngineContext context, Exception exception) {
            ErrorMessages(context, 500, exception.Message);
            context.GetLogger().Error("Error on " + context.UrlInfo.UrlRaw + " for user " + context.CurrentUser.Identity.Name,
                                     exception);
        }

        public static bool Handle400(this IEngineContext context) {
            return HandleError(context, 400, "Bad request.");
        }

        public static bool Handle404(this IEngineContext context) {
            var result = context.HandleError(404, "Not found.");
            context.Response.StatusCode = 404;
            return result;
        }

        public static bool Handle403(this IEngineContext context) {
            return HandleError(context, 403, "You do not have enough priviledges.");
        }

        public static bool Handle405(this IEngineContext context) {
            return HandleError(context, 405, "Method not allowed.");
        }

        public static bool HandleError(this IEngineContext context, HttpStatusCode code, string message) {
            return HandleError(context, (int)code, new MonoRailException(message));
        }

        public static bool HandleError(this IEngineContext context, int code, string message) {
            return HandleError(context, code, new MonoRailException(message));
        }
        public static bool HandleError(this IEngineContext context, HttpStatusCode code, Exception exception) {
            return HandleError(context, (int)code, exception);
        }

        public static bool HandleError(this IEngineContext context, int code, Exception exception) {
            context.LastException = exception;
            context.CurrentControllerContext.SelectedViewName = "/rescues/generalerror";
            ErrorMessages(context, code, exception.Message);
            var logger = GetLogger(context);
            logger.Error("Error on " + context.UrlInfo.UrlRaw + " for user " + context.CurrentUser.Identity.Name, exception);

            return false;
        }

        public static void SetupDownload(this IEngineContext context, string filename, string file) {
            using (var fs = File.OpenRead(file))
                SetupDownload(context, fs, filename, string.Empty, 0);
        }

        public static void SetupDownload(this IEngineContext context, string filename, byte[] data) {
            SetupDownload(context, filename, string.Empty, data);
        }

        public static void SetupDownload(this IEngineContext context, string filename, string etag, byte[] data, int days = 0) {
            using (var ms = new MemoryStream(data)) {
                SetupDownload(context, ms, filename, etag, days);
            }
        }

        public static void SetupDownload(this IEngineContext context, Stream data, string filename, string etag, int days = 0) {
            context.CurrentControllerContext.SelectedViewName = null;
            context.CurrentControllerContext.LayoutNames = null;
            var response = context.UnderlyingContext.Response;
            response.Clear();
            if (!string.IsNullOrEmpty(etag) && etag.Equals(context.Request.Headers[Header.IfNoneMatch])) {
                context.Response.StatusCode = 304;
            } else {
                if (!string.IsNullOrEmpty(filename))
                    response.AppendHeader(Header.ContentDisposition, "attachment; filename=\"" + filename + "\"");

                response.ContentType = MimeMapping.GetMimeMapping(filename);

                if (etag != null) {
                    response.AppendHeader(Header.ETag, etag);
                    response.AppendHeader(Header.CacheControl, "public");
                }
                if (days > 0) response.AppendHeader(Header.Expires, DateTime.Now.Date.AddDays(days).ToString("R"));

                //response.AddHeader(ContentLength, data.Length.ToString(CultureInfo.InvariantCulture));

                data.CopyTo(response.OutputStream);
            }
        }

        public static void RenderAndDeliverMail(this IEngineContext context, string template) {
            RenderAndDeliverMail(context, template, null);
        }

        public static void RenderAndDeliverMail(this IEngineContext context, string template, IEnumerable<MailAddress> cc) {
            var dict = DictHelper.Create();
            foreach(var key in context.CurrentControllerContext.PropertyBag.Keys) {
                dict[key] = context.CurrentControllerContext.PropertyBag[key];
            }
            foreach (var key in context.CurrentControllerContext.Helpers.Keys) {
                dict[key] = context.CurrentControllerContext.Helpers[key];
            }
            var mail = context.Services.EmailTemplateService.RenderMailMessage(template, "empty", dict);
            if (cc != null)
                foreach (var address in cc)
                    mail.CC.Add(address);

            context.Services.EmailSender.Send(mail);
        }
    }
}
