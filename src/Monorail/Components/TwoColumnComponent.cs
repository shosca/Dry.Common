#region using

using Castle.MonoRail.Framework;

#endregion

namespace Dry.Common.Monorail.Components {
    [ViewComponentDetails("TwoColumn", Sections = "anouncement, headerimage, title, content, column", Cache = ViewComponentCache.Always)]
    public class TwoColumnComponent : ViewComponent {
        public override void Initialize() {
            PropertyBag["Context"] = Context;
        }
    }
}
