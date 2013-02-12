#region using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;

#endregion

namespace Dry.Common.Monorail.Controllers {
    public class SpellCheckController : BaseController {
        public void Index(string text, string suggest, string lang) {
            var items = new HashSet<string>();
            if (!string.IsNullOrEmpty(text)) {
                var t = Context.Server.UrlDecode(text);
                items.AddRange(GetListOfMisspelledWords(t, lang));
            } else if (!string.IsNullOrEmpty(suggest)) {
                items.AddRange(GetListOfSuggestedWords(suggest, lang));
            }
            PropertyBag["items"] = items;
        }

        static IEnumerable<string> GetListOfMisspelledWords(string text, string lang) {
            if (string.IsNullOrEmpty(text)) {
                return null;
            }
            var xdoc = new XmlDocument();
            xdoc.LoadXml(AskGoogle(text, lang));
            if (!xdoc.HasChildNodes) {
                return null;
            }
            var nodeList = xdoc.SelectNodes("//c");
            if (null == nodeList || 0 >= nodeList.Count) {
                return null;
            }
            return (from XmlNode node in nodeList
                let offset = Convert.ToInt32(node.Attributes["o"].Value)
                let length = Convert.ToInt32(node.Attributes["l"].Value)
                select text.Substring(offset, length)).ToList();
        }

        static IEnumerable<string> GetListOfSuggestedWords(string suggest, string lang) {
            if (string.IsNullOrEmpty(suggest)) {
                return null;
            }
            var xdoc = new XmlDocument();
            xdoc.LoadXml(AskGoogle(suggest, lang));
            if (!xdoc.HasChildNodes) {
                return null;
            }
            var nodeList = xdoc.SelectNodes("//c");
            if (null == nodeList || 0 >= nodeList.Count) {
                return null;
            }
            var list = new List<string>();
            foreach (XmlNode node in nodeList) {
                list.AddRange(node.InnerText.Split('\t'));
            }
            return list;
        }

        static string AskGoogle(string text, string lang) {
            var uri = "https://www.google.com/tbproxy/spell?lang=" + lang + "&hl=en";
            using (var wc = new WebClient()) {
                var body = new StringBuilder();
                body.Append("<?xml version='1.0' encoding='utf-8' ?>");
                body.Append("<spellrequest textalreadyclipped=\"0\" ignoredups=\"0\" ignoredigits=\"1\" ignoreallcaps=\"1\">");
                body.Append("<text>");
                body.Append(text);
                body.Append("</text>");
                body.Append("</spellrequest>");
                var postdata = body.ToString();
                wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                var bytes = Encoding.ASCII.GetBytes(postdata);
                var response = wc.UploadData(uri, "POST", bytes);
                return Encoding.ASCII.GetString(response);
            }
        }
    }
}
