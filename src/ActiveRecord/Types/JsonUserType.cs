#region using

using System;
using System.Data;
using System.Linq;
using Castle.ActiveRecord;
using NHibernate;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;
using ServiceStack.Text;

#endregion

namespace Dry.Common.ActiveRecord.Types {
    public static class TypeNameHelper {
        public static string GetSimpleTypeName(object obj) {
            return null == obj
                       ? null
                       : obj.GetType().AssemblyQualifiedName;
        }

        public static Type GetType(string simpleTypeName) {
            return Type.GetType(simpleTypeName);
        }
    }

    public class JsonUserType<T> : IUserType {
        static object Deserialize(string data) {
            return JsonSerializer.DeserializeFromString<T>(data);
        }

        static string Serialize(object value) {
            return null == value
                       ? null
                       : JsonSerializer.SerializeToString(value);
        }

        static string GetType(object value) {
            return null == value
                       ? null
                       : TypeNameHelper.GetSimpleTypeName(value);
        }

        public object NullSafeGet(IDataReader rs, string[] names, object owner) {
            int dataindex = rs.GetOrdinal(names[0]);
            if (rs.IsDBNull(dataindex)) return null;

            var data = (string) rs.GetValue(dataindex);
            return Deserialize(data);
        }

        public void NullSafeSet(IDbCommand cmd, object value, int index) {
            if (value == null) {
                NHibernateUtil.String.NullSafeSet(cmd, null, index);
                return;
            }

            var data = Serialize(value);
            NHibernateUtil.String.NullSafeSet(cmd, data, index);
        }

        public object DeepCopy(object value) {
            return value == null
                       ? null
                       : Deserialize(Serialize(value));
        }

        public object Replace(object original, object target, object owner) {
            return original;
        }

        public object Assemble(object cached, object owner) {
            var parts = cached as string[];
            return parts == null
                       ? null
                       : Deserialize(parts[1]);
        }

        public object Disassemble(object value) {
            return (value == null)
                       ? null
                       : new[] {GetType(value), Serialize(value)};
        }

        public SqlType[] SqlTypes {
            get {
                return new[] {
                                 SqlTypeFactory.GetStringClob(Int16.MaxValue)
                             };
            }
        }

        public new bool Equals(object x, object y) {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(null, x) || ReferenceEquals(null, y)) return false;
            return Serialize(x).Equals(Serialize(y));
        }

        public int GetHashCode(object x) {
            return (x == null) ? 0 : x.GetHashCode();
        }

        public Type ReturnedType {
            get { return typeof (T); }
        }

        public bool IsMutable {
            get { return false; }
        }
    }
}
