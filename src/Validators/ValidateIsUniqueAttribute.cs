using System;
using Castle.Components.Validator;

namespace Dry.Common.Validators {
    [Serializable]
    public class ValidateIsUniqueAttribute : AbstractValidationAttribute {
        readonly IValidator _validator;

        public ValidateIsUniqueAttribute() {
            _validator = new IsUniqueValidator();
        }

        public ValidateIsUniqueAttribute(string errormessage) : base(errormessage) {
            _validator = new IsUniqueValidator();
        }

        public override IValidator Build() {
            ConfigureValidatorMessage(_validator);
            return _validator;
        }
    }
}