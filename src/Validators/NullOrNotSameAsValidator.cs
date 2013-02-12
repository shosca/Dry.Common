using System;
using Castle.Components.Validator;

namespace Dry.Common.Validators {
    [Serializable]
    public class NullOrNotSameAsValidator : NotSameAsValidator {
        public NullOrNotSameAsValidator(string propertyToCompare) : base(propertyToCompare) {}

        public override bool IsValid(object instance, object fieldValue) {
            if (fieldValue == null) return true;
            return base.IsValid(instance, fieldValue);
        }
    }
}