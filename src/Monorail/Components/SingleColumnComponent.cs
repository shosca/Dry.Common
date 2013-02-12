#region using

using Castle.MonoRail.Framework;

#endregion

namespace Dry.Common.Monorail.Components {
    [ViewComponentDetails("SingleColumn", Sections = "anouncement, headerimage, title, content", Cache = ViewComponentCache.Always)]
    public class SingleColumnComponent : ViewComponent {
        public override void Initialize() {
            PropertyBag["Context"] = Context;
        }
    }
}
