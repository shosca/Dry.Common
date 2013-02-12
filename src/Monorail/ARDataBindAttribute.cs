using System;
using System.Reflection;
using Castle.Components.Binder;
using Castle.MonoRail.Framework;
using Dry.Common.ActiveRecord;

namespace Dry.Common.Monorail {
    public enum AutoLoadBehavior
    {
        /// <summary>
        /// Means that no autoload should be performed on the target
        /// type or on nested types.
        /// </summary>
        Never,

        /// <summary>
        /// Means that autoload should be used for the target type
        /// and the nested types (if present). This demands that
        /// the primary key be present on the http request
        /// </summary>
        Always,

        /// <summary>
        /// Does not load the root type, but loads nested types
        /// if the primary key is present. If not present, sets null on nested type.
        /// </summary>
        OnlyNested,

        /// <summary>
        /// Means that we should autoload, but if the key is
        /// invalid, like <c>null</c>, 0 or an empty string, then just
        /// create a new instance of the target type.
        /// </summary>
        NewInstanceIfInvalidKey,

        /// <summary>
        /// Means that we should autoload target and nested types when the key is valid.
        /// If the key is invalid, like <c>null</c>, 0 or an empty string, and the
        /// instance is the root instance, then create a new instance of the target type.
        /// If the key is invalid, and it's a nested instance, then set null on the nested type.
        /// </summary>
        NewRootInstanceIfInvalidKey,

        /// <summary>
        /// Means that we should autoload, but if the key is
        /// invalid, like <c>null</c>, 0 or an empty string, then just
        /// return null
        /// </summary>
        NullIfInvalidKey
    }

    [AttributeUsage(AttributeTargets.Parameter), Serializable]
    public class ARDataBindAttribute : DataBindAttribute {
        public ARDataBindAttribute(string prefix) : base(prefix) {
            AutoLoad = AutoLoadBehavior.Never;
            Expect = null;
            TreatEmptyGuidAsNull = true;
        }

        public ARDataBindAttribute(string prefix, AutoLoadBehavior autoLoadBehavior) : base(prefix) {
            AutoLoad = autoLoadBehavior;
            Expect = null;
            TreatEmptyGuidAsNull = true;
        }

        public AutoLoadBehavior AutoLoad { get; private set; }

        public string Expect { get; set; }

        public bool TreatEmptyGuidAsNull { get; set; }

        public override object Bind(IEngineContext context, IController controller, IControllerContext controllerContext, ParameterInfo parameterInfo) {
            var binder = (ARDataBinder) CreateBinder();
            var validatorAccessor = controller as IValidatorAccessor;

            ConfigureValidator(validatorAccessor, binder);

            binder.AutoLoad = AutoLoad;
            binder.TreatEmptyGuidAsNull = TreatEmptyGuidAsNull;

            var node = context.Request.ObtainParamsNode(From);

            var instance = binder.BindObject(parameterInfo.ParameterType, Prefix, Exclude, Allow, Expect, node);

            BindInstanceErrors(validatorAccessor, binder, instance);
            PopulateValidatorErrorSummary(validatorAccessor, binder, instance);

            return instance;
        }

        protected override IDataBinder CreateBinder() {
            return new ARDataBinder();
        }
    }
}
