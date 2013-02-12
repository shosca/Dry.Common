using System;
using System.Reflection;
using Castle.Components.Binder;
using Castle.MonoRail.Framework;
using Dry.Common.ActiveRecord;

namespace Dry.Common.Monorail
{
    [AttributeUsage(AttributeTargets.Parameter), Serializable]
    public class ARFetchAttribute : Attribute, IParameterBinder {
        private IDataBinder binder;

        public ARFetchAttribute(string requestParameterName, bool create, bool required) : base() {
            RequestParameterName = requestParameterName;
            Create = create;
            Required = required;
        }

        public ARFetchAttribute() : this(null, false, false) { }

        public ARFetchAttribute(string requestParameterName) : this(requestParameterName, false, false) { }

        public ARFetchAttribute(bool create, bool require) : this(null, create, require) { }

        public string RequestParameterName { get; set; }

        public bool Create { get; set; }

        public bool Required { get; set; }

        public string Eager { get; set; }

        public virtual int CalculateParamPoints(IEngineContext context, IController controller, IControllerContext controllerContext, ParameterInfo parameterInfo) {
            var paramName = RequestParameterName ?? parameterInfo.Name;
            return context.Request.Params.Get(paramName) != null ? 10 : 0;
        }

        public virtual object Bind(IEngineContext context, IController controller, IControllerContext controllerContext, ParameterInfo parameterInfo) {
            EnsureBinderExists();
            var fetcher = new ARFetcher(binder.Converter);
            return fetcher.FetchActiveRecord(parameterInfo, this, context.Request.Params, controllerContext.CustomActionParameters);
        }

        private void EnsureBinderExists() {
            if (binder == null) {
                binder = CreateBinder();
            }
        }

        protected virtual IDataBinder CreateBinder() {
            return new DataBinder();
        }
    }
}
