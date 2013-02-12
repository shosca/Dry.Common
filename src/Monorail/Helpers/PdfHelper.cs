#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Castle.Core.Logging;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Helpers;

#endregion

namespace Dry.Common.Monorail.Helpers {
    public class PdfHelper : AbstractHelper {
        ILogger _logger = NullLogger.Instance;
        public ILogger Logger {
            get { return _logger; }
            set { _logger = value; }
        }

        public string WkHtmlPath { get; private set; }

        public PdfHelper() {}

        public PdfHelper(IEngineContext context, string wkhtmlpath) : base(context) {
            WkHtmlPath = context.Server.MapPath(wkhtmlpath);
        }
        public string RenderToPdf(string view) {
            return RenderToPdf(null, view);
        }

        public string RenderToPdf(string layout, string view) {
            return RenderToPdf(null, view, false);
        }

        public string RenderToPdf(string layout, string view, bool islandscape) {
            var temp = Path.GetTempPath() + Guid.NewGuid();
            var tempfile = temp + ".html";
            var outfile = temp + ".pdf";
            
            try {
                using (TextWriter writer = new StreamWriter(tempfile)) {
                    var dict = new Dictionary<string, object>();
                    foreach(var key in ControllerContext.PropertyBag.Keys) {
                        dict.Add(key.ToString(), ControllerContext.PropertyBag[key]);
                    }
                    Context.Services.ViewEngineManager.Process(view, layout, writer, dict);

                    writer.Close();
                }

                var p = new Process {
                        StartInfo = {
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            FileName = WkHtmlPath,
                            Arguments = (islandscape ? "-O Lanscape" : "") + "\"" + tempfile + "\" \"" + outfile + "\""
                    }
                };
                p.Start();
                p.WaitForExit();
                return outfile;
            } catch(Exception e) {
                Logger.Error("Error generating pdf.", e);
                throw;
            } finally {
            }
        }
    }
}
