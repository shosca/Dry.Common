#region using

using System;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using NHibernate;
using NHibernate.Classic;
using NHibernate.Criterion;

#endregion

namespace Dry.Common.Validators {
    [Serializable]
    public class IsNullOrUniqueValidator : AbstractValidator {
        public override bool IsValid(object instance, object fieldvalue) {
            if (fieldvalue == null) return true;
            var instanceType = NHibernateUtil.GetClass(instance);
            var model = AR.Holder.GetModel(instanceType);

            if (model == null) {
                throw new ValidationFailure("Couldn't figure out the primary key for " + instanceType.FullName +
                                            " so can't ensure the uniqueness of any field. Validator failed.");
            }

            return AR.Execute(instanceType, session => {
                var origflushmode = session.FlushMode;
                session.FlushMode = FlushMode.Never;
                
                try {
                    var criteria = session.CreateCriteria(model.Type)
                        .SetProjection(Projections.RowCount());

                    if (Property.Name.Equals(model.PrimaryKey.Key, StringComparison.InvariantCultureIgnoreCase)) {
                        // IsUniqueValidator is on the PrimaryKey Property, simplify query
                        criteria.Add(Restrictions.Eq(Property.Name, fieldvalue));
                    } else {
                        var id = instance.GetType().GetProperty(model.PrimaryKey.Key).GetValue(instance, null);
                        ICriterion pKeyCriteria = (id == null)
                                                    ? Restrictions.IsNull(model.PrimaryKey.Key)
                                                    : Restrictions.Eq(model.PrimaryKey.Key, id);

                        criteria.Add(Restrictions.And(Restrictions.Eq(Property.Name, fieldvalue), Restrictions.Not(pKeyCriteria)));
                    }

                    return criteria.UniqueResult<int>() == 0;

                } finally {
                    session.FlushMode = origflushmode;
                }
            });
        }

        public override bool SupportsBrowserValidation {
            get { return false; }
        }

        protected override string BuildErrorMessage() {
            return String.Format("{0} is currently in use. Please pick up a new {0}.", Property.Name);
        }
    }
}
