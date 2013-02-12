#region using

using System;
using System.Linq.Expressions;
using System.Reflection;

#endregion

namespace Dry.Common {
    public static class Clr<T> {
        public static string Name(Expression<Func<T, object>> expression) {
            return Info(expression).Name;
        }

        public static PropertyInfo Info(Expression<Func<T, object>> expression) {
            var memberaccess = expression.Body as MemberExpression;
            if (memberaccess != null) return memberaccess.Member as PropertyInfo;
            var unary = expression.Body as UnaryExpression;
            if (unary != null) return ((MemberExpression) unary.Operand).Member as PropertyInfo;
            return null;
        }
    }
}
