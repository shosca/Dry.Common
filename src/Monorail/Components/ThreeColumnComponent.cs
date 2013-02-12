#region using

using Castle.MonoRail.Framework;

#endregion

namespace Dry.Common.Monorail.Components {
    [ViewComponentDetails("ThreeColumn", Sections = "anouncement, headerimage, title, content, column, leftcolumn", Cache = ViewComponentCache.Always)]
    public class ThreeColumnComponent : ViewComponent {
        public override void Initialize() {
            PropertyBag["Context"] = Context;
        }
    }
}