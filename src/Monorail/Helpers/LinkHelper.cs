#region using

using System;
using System.Collections;
using System.Collections.Generic;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Helpers;
using Castle.MonoRail.Framework.Services;

#endregion

namespace Dry.Common.Monorail.Helpers {
    public class LinkHelper : AbstractHelper {
        public LinkHelper() { }
        public LinkHelper(IEngineContext context) : base(context) { }

        public IUrlBuilder UrlBuilder { get; set; }
        public UrlInfo CurrentUrl { get; set; }

        public override void SetContext(IEngineContext context)
        {
            base.SetContext(context);

            UrlBuilder = (IUrlBuilder) context.GetService(typeof (IUrlBuilder));
            CurrentUrl = context.UrlInfo;
        }
        
        public string For(string suffix) {
            return For(suffix, null);
        }

        public string For(string suffix, IDictionary dict) {
            return For(Context.CurrentController.GetType(), suffix, dict);
        }
        
        public string For() {
            return For(DictHelper.Create());
        }

        public string For(IDictionary dict) {
            if (dict == null) dict = DictHelper.Create();
            var suffix = GenerateSuffix(dict);
            return For(Context.CurrentController.GetType(), suffix, dict);
        }
        
        public string For(Type type) {
            return For(type, DictHelper.Create());
        }

        public string For(Type type, IDictionary dict) {
            if (dict == null) dict = DictHelper.Create();
            var suffix = GenerateSuffix(dict);
            return For(type, suffix, dict);
        }
        
        public string For(Type type, string suffix) {
            return For(type, suffix, null);
        }

        public string For(Type type, string suffix, IDictionary dict) {
            if (type == null) return string.Empty;

            if (dict == null) dict = DictHelper.Create();

            var area = type.GetAreaName();
            dict["area"] = area;
            dict["controller"] = type.GetControllerName();
            dict["route"] = area + suffix;

            return BuildUrl(dict);
        }

        string BuildUrl(IDictionary parameters) {
            var urlparams = UrlBuilderParameters.From(parameters).SetRouteMatch(ControllerContext.RouteMatch);
            urlparams.EncodeForLink = true;

            return UrlBuilder.BuildUrl(CurrentUrl, urlparams);
        }

        string GenerateSuffix(IDictionary dict) {
            var suffix = new List<string>(new [] { "+" });
            if (dict.Contains("params")) {
                var paramsdict = dict["params"] as IDictionary;
                if (paramsdict != null) {
                    if (paramsdict.Contains("areaid") && paramsdict["areaid"] != null) {
                        suffix.Add("areaid");
                    }
                    if (paramsdict.Contains("id") && paramsdict["id"] != null) {
                        var guid = Guid.Empty;
                        suffix.Add(Guid.TryParse(paramsdict["id"].ToString(), out guid) ? "guid" : "id");
                    }
                }
            }
            suffix.Add("action");
            return suffix.Join("/");
        }
    }
}