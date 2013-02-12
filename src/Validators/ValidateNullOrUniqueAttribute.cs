#region using

using System;
using Castle.Components.Validator;

#endregion

namespace Dry.Common.Validators {
    [Serializable]
    public class ValidateNullOrUniqueAttribute : AbstractValidationAttribute {
        readonly IValidator _validator;

        public ValidateNullOrUniqueAttribute() {
            _validator = new IsNullOrUniqueValidator();
        }

        public ValidateNullOrUniqueAttribute(string errorMessage) : base(errorMessage) {
            _validator = new IsNullOrUniqueValidator();
        }

        public override IValidator Build() {
            ConfigureValidatorMessage(_validator);
            return _validator;
        }
    }
}
