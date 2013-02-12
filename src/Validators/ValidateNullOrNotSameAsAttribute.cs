#region using

using System;
using Castle.Components.Validator;

#endregion

namespace Dry.Common.Validators {
    [Serializable]
    public class ValidateNullOrNotSameAsAttribute : ValidateNotSameAsAttribute {
        readonly string _prop;

        public ValidateNullOrNotSameAsAttribute(string propertyToCompare) : base(propertyToCompare) {
            _prop = propertyToCompare;
        }

        public ValidateNullOrNotSameAsAttribute(string propertyToCompare, string errorMessage)
            : base(propertyToCompare, errorMessage) {
            _prop = propertyToCompare;
        }

        public override IValidator Build() {
            var validator = new NullOrNotSameAsValidator(_prop);
            ConfigureValidatorMessage(validator);
            return validator;
        }
    }
}
