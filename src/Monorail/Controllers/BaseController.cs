#region using

using System;
using Castle.Components.Binder;
using Castle.MonoRail.Framework;
using Dry.Common.Monorail.Filters;
using Dry.Common.Monorail.Helpers;

#endregion

namespace Dry.Common.Monorail.Controllers {
    [Layout("default")]
    [Rescue("generalerror")]
    [Helper(typeof (ScriptHelper))]
    [Helper(typeof (DateHelper))]
    [Helper(typeof (PdfHelper))]
    [Helper(typeof (LinkHelper), "Link")]
    [Filter(ExecuteWhen.AfterAction, typeof (WhitespaceFilter))]
    [Filter(ExecuteWhen.Always, typeof (TracingFilter))]
    [Filter(ExecuteWhen.BeforeAction, typeof (ConfigurationFilter), ExecutionOrder = 0)]
    [Filter(ExecuteWhen.BeforeAction, typeof (GZipFilter), ExecutionOrder = 0)]
    [Filter(ExecuteWhen.AfterAction, typeof (ViewFilter))]
    public abstract class BaseController : SmartDispatcherController {
        #region from ARSmartDispatcherController
        public BaseController() : base(new Dry.Common.ActiveRecord.ARDataBinder()) {}
        public BaseController(IDataBinder binder) : base(binder) {}
        protected object BindObject(ParamStore from, Type targetType, String prefix, String excludedProperties, String allowedProperties, AutoLoadBehavior autoLoad) {
            SetAutoLoadBehavior(autoLoad);
            return BindObject(from, targetType, prefix, excludedProperties, allowedProperties);
        }
        protected object BindObject(ParamStore from, Type targetType, String prefix, AutoLoadBehavior autoLoad) {
            SetAutoLoadBehavior(autoLoad);
            return BindObject(from, targetType, prefix);
        }
        protected void BindObjectInstance(object instance, ParamStore from, String prefix, AutoLoadBehavior autoLoad) {
            SetAutoLoadBehavior(autoLoad);
            BindObjectInstance(instance, from, prefix);
        }
        protected void BindObjectInstance(object instance, String prefix, AutoLoadBehavior autoLoad) {
            SetAutoLoadBehavior(autoLoad);
            BindObjectInstance(instance, ParamStore.Params, prefix);
        }

        protected void SetAutoLoadBehavior(AutoLoadBehavior autoLoad) {
            var binder = (Dry.Common.ActiveRecord.ARDataBinder) Binder;
            binder.AutoLoad = autoLoad;
        }
        #endregion

        public virtual void Index() {
            if (DynamicActions.ContainsKey("index"))
                DynamicActions["index"].Execute(Context, this, ControllerContext);
        }

        public bool IsAjax {
            get { return Context.IsAjax(); }
        }

        protected void SuccessMessage(string message) {
            Context.SuccessMessage(message);
        }

        protected void ErrorMessages(params string[] message) {
            Context.ErrorMessages(message);
        }

        protected void ErrorMessages(Exception e) {
            Context.ErrorMessages(e);
        }
    }
}
