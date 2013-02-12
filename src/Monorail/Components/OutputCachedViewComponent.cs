#region using

using System.IO;
using Castle.MonoRail.Framework;
using Dry.Common.Cache;

#endregion

namespace Dry.Common.Monorail.Components {
    [ViewComponentDetails("Cached", Cache = ViewComponentCache.Always)]
    public class OutputCachedViewComponent : ViewComponent {
        ICache _cache = NullCache.Instance;
        public ICache Cache {
            get { return _cache; }
            set { _cache = value; }
        }

        [ViewComponentParam(Required = true)]
        public string Key { get; set; }

        [ViewComponentParam]
        public int Duration { get; set; }

        public override void Initialize() {
            PropertyBag["Context"] = Context;
        }

        public override void Render() {
            Context.ViewToRender = null;
#if DEBUG
            RenderText(InternalRender());
#else
            if (Settings.Get("build").Equals("release")) {
                RenderText(Cache.Get(Key, Duration < 1 ? 800 : Duration, InternalRender) as string);
            } else {
                RenderText(InternalRender());
            }
#endif
        }

        string InternalRender() {
            var writer = new StringWriter();
            Context.RenderBody(writer);
            return writer.GetStringBuilder().ToString();
        }
    }
}
