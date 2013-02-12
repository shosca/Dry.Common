#region using

using System.Collections;
using System.Reflection;
using System.Web;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Configuration;

#endregion

namespace Dry.Common.Monorail.Extensions {
    public class WebTraceExtension : IMonoRailExtension {
        bool _includePropertyBag;
        bool _htmlOnly = true;

        public void SetExtensionConfigNode(Castle.Core.Configuration.IConfiguration node) {}

        public void Service(IMonoRailServices serviceProvider) {
            var config = serviceProvider.GetService<IMonoRailConfiguration>();
            var webtraceNode = config.ConfigurationSection.Children["webtrace"];
            var attr = webtraceNode.Attributes["enabled"];
            var enabled = attr != null && System.Xml.XmlConvert.ToBoolean(attr);
            if (!enabled) return;

            var manager = serviceProvider.GetService<ExtensionManager>();
            manager.PostControllerProcess += manager_PostControllerProcess;

            attr = webtraceNode.Attributes["includePropertyBag"];
            _includePropertyBag = (attr != null) && System.Xml.XmlConvert.ToBoolean(attr);

            attr = webtraceNode.Attributes["htmlOnly"];
            _htmlOnly = (attr == null) || System.Xml.XmlConvert.ToBoolean(attr);
        }

        static void DictionaryToTable(System.Data.DataSet renderdata, string sectionName, IDictionary dict) {
            var applicationTable = renderdata.Tables[sectionName + "_State"];
            foreach (System.Collections.DictionaryEntry kv in dict) {
                var row = applicationTable.NewRow();
                var current = kv.Key as string;
                row[sectionName + "_Key"] = current ?? "<null>";
                var obj2 = kv.Value;
                if (obj2 != null) {
                    row["Trace_Type"] = obj2.GetType();
                    row["Trace_Value"] = obj2.ToString();
                }
                else {
                    row["Trace_Type"] = "<null>";
                    row["Trace_Value"] = "<null>";
                }
                applicationTable.Rows.Add(row);
            }
        }

        void manager_PostControllerProcess(IEngineContext context) {
            // only insert on Html pages.
            if (_htmlOnly &&
                (!context.Response.ContentType.StartsWith("text/html") ||
                 context.Request.Headers["x-requested-with"] == "XMLHttpRequest"))
                return;

            TraceContext tc = context.UnderlyingContext.Trace ?? new TraceContext(context.UnderlyingContext);
            if (_includePropertyBag) {
                var getdata = typeof (TraceContext).GetMethod("GetData",
                                                              BindingFlags.NonPublic | BindingFlags.Instance |
                                                              BindingFlags.InvokeMethod);
                var renderdata = getdata.Invoke(tc, null) as System.Data.DataSet;
                if (renderdata != null) {
                    DictionaryToTable(renderdata, "Trace_Application", context.CurrentControllerContext.PropertyBag);
                    DictionaryToTable(renderdata, "Trace_Session", context.Flash);
                }
            }

            var render = typeof (TraceContext).GetMethod("Render",
                                                         BindingFlags.NonPublic | BindingFlags.Instance |
                                                         BindingFlags.InvokeMethod);
            using (var htw = new System.Web.UI.HtmlTextWriter(context.UnderlyingContext.Response.Output)) {
                render.Invoke(tc, new object[] {htw});
            }
        }
    }
}
