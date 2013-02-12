#region using

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Castle.ActiveRecord;
using Dry.Common;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;

#endregion

namespace Dry.Common.ActiveRecord.Types {
    public class DelimitedList : IUserType {
        const string Delimiter = "|";

        public new bool Equals(object x, object y) {
            return object.Equals(x, y);
        }

        public int GetHashCode(object x) {
            return x.GetHashCode();
        }

        public object NullSafeGet(IDataReader rs, string[] names, object owner) {
            var r = rs[names[0]];
            return r == DBNull.Value
                       ? new SortedSet<string>()
                       : new SortedSet<string>(((string) r).SplitAndTrim(new[] {Delimiter}).ToList());
        }

        public void NullSafeSet(IDbCommand cmd, object value, int index) {
            object paramVal = DBNull.Value;
            if (value != null) {
                paramVal = string.Join(Delimiter, ((IEnumerable<string>) value));
            }
            var parameter = (IDataParameter) cmd.Parameters[index];
            parameter.Value = paramVal;
        }

        public object DeepCopy(object value) {
            return value;
        }

        public object Replace(object orig, object target, object owner) {
            return orig;
        }

        public object Assemble(object cached, object owner) {
            return cached;
        }

        public object Disassemble(object value) {
            return value;
        }

        public SqlType[] SqlTypes {
            get { return new SqlType[] {new StringSqlType()}; }
        }

        public Type ReturnedType {
            get { return typeof (SortedSet<string>); }
        }

        public bool IsMutable {
            get { return false; }
        }
    }
}
