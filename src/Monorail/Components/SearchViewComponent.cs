#region using

using Castle.MonoRail.Framework;
using Dry.Common.Monorail.Search;

#endregion

namespace Dry.Common.Monorail.Components {
    public class SearchViewComponent : ViewComponent {
        [ViewComponentParam]
        public ISiteSearch Result { get; set; } 

        public override void Initialize() {
            PropertyBag["Context"] = Context;
        }
    }
}
