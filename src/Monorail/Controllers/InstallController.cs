#region using

using Castle.MonoRail.Framework;
using Dry.Common.Monorail.Filters;
using Dry.Common.Monorail.Helpers;
using Dry.Common.ActiveRecord;

#endregion

namespace Dry.Common.Monorail.Controllers {
    public class InstallController : BaseController {
        [SkipFilter(typeof(ConfigurationFilter))]
        [SkipFilter(typeof(WhitespaceFilter))]
        public override void Index() {
            base.Index();
        }

#if DEBUG
        [SkipFilter(typeof(ConfigurationFilter))]
        [SkipFilter(typeof(WhitespaceFilter))]
        [AccessibleThrough(Verb.Post)]
        public void Create() {
            try {
                ARHelper.CreateSchema(Context);
                Context.SuccessMessage("Schema has been created successfully.");
            } catch (System.Exception e) {
                ErrorMessages(e);
            }
            RedirectToAction("index");
        }

        [SkipFilter(typeof(ConfigurationFilter))]
        [SkipFilter(typeof(WhitespaceFilter))]
        [AccessibleThrough(Verb.Post)]
        public void Drop() {
            try {
                ARHelper.DropSchema();
                Context.SuccessMessage("Schema has been dropped successfully.");
            } catch (System.Exception e) {
                ErrorMessages(e);
            }
            RedirectToAction("index");
        }
#endif
        [SkipFilter(typeof(ConfigurationFilter))]
        [SkipFilter(typeof(WhitespaceFilter))]
        [AccessibleThrough(Verb.Post)]
        public void CreateScript() {
            try {
                ARHelper.GenerateCreationScripts(Context.Server.MapPath("~/schema.sql"));
                Context.SuccessMessage("Schema script has been created successfully.");
            } catch (System.Exception e) {
                ErrorMessages(e);
            }
            RedirectToAction("index");
        }
    }
}
