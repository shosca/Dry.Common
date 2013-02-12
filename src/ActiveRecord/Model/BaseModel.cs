#region using

using System.Collections.Generic;
using System.Text;
using Castle.ActiveRecord;
using NHibernate;
using NHibernate.Type;

#endregion

namespace Dry.Common.ActiveRecord.Model {
    public abstract class BaseModel<T> : ActiveRecordBase<T> where T : class {
        public override string ToString() {
            var model = AR.Holder.GetClassMetadata(GetType());
            return new StringBuilder("<")
                .Append(GetType().Name)
                .Append("#")
                .Append(model.GetIdentifier(this, EntityMode.Poco))
                .Append(":")
                .Append(Collect())
                .Append(">").ToString();
        }

        protected string Collect() {
            var model = AR.Holder.GetClassMetadata(GetType());

            var vals = new List<string>();
            foreach (var prop in model.PropertyNames) {
                var proptype = model.GetPropertyType(prop);
                if (proptype is StringType && !(proptype is StringClobType)) {
                    try {
                        vals.Add(GetType().GetProperty(prop).GetValue(this, null).ToString());
                    } catch { /* swallow */ }
                }
            }
            return string.Join(" ", vals);
        }
    }
}
