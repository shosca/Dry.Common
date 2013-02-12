#region using

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Dry.Common;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.SqlCommand;

#endregion

namespace Dry.Common.ActiveRecord
{
    public static class QueryExtensions
    {
        public static ICriteria SetFetchMode<T>(this ICriteria criteria, Expression<Func<T, object>> expression, FetchMode fetchmode) {
            return criteria.SetFetchMode(Clr<T>.Name(expression), fetchmode);
        }

        public static DetachedCriteria SetFetchMode<T>(this DetachedCriteria criteria, Expression<Func<T, object>> expression, FetchMode fetchmode) {
            return criteria.SetFetchMode(Clr<T>.Name(expression), fetchmode);
        }

        public static ICriteria CreateCriteria<T>(this ICriteria criteria, Expression<Func<T, object>> expression) {
            return criteria.CreateCriteria(Clr<T>.Name(expression));
        }

        public static DetachedCriteria CreateCriteria<T>(this DetachedCriteria criteria, Expression<Func<T, object>> expression) {
            return criteria.CreateCriteria(Clr<T>.Name(expression));
        }

        public static SqlString Select(this SqlString s) {
            return s.Append("select ");
        }

        public static SqlString Column<T>(this SqlString s, Expression<Func<T, object>> exp) {
            return s.Append(Clr<T>.Name(exp).ToLower());
        }


        public static SqlString Select<T>(this SqlString s, Expression<Func<T, object>> exp) {
            return s.Select().Column(exp);
        }

        public static SqlString And<T>(this SqlString s, Expression<Func<T, object>> exp) {
            return s.Append(", ").Column(exp);
        }

        public static SqlString From(this SqlString s) {
            return s.Append(" from ");
        }

        public static SqlString From<T>(this SqlString s) where T : class {
            return s.From().Append(Dry.Common.ActiveRecord.Mapping.Conventions.TableName<T>());
        }

        public static SqlString Where(this SqlString s) {
            return s.Append(" where ");
        }

        public static SqlString Where<T>(this SqlString s, Expression<Func<T, object>> exp) {
            return s.Append(" where ").Column(exp);
        }

        public static SqlString Eq(this SqlString s) {
            return s.Append("=");
        }

        public static SqlString Eq(this SqlString s, object val) {
            return s.Eq().Append(val.ToString());
        }

        public static SqlString OrderBy(this SqlString s) {
            return s.Append(" order by ");
        }

        public static SqlString OrderBy<T>(this SqlString s, Expression<Func<T, object>> exp) {
            return s.OrderBy().Column(exp);
        }

        public static SqlString Desc(this SqlString s) {
            return s.Append(" desc ");
        }

        public static SqlString Asc(this SqlString s) {
            return s.Append(" desc ");
        }

        public static SqlString Update(this SqlString s) {
            return s.Append("update ");
        }

        public static SqlString Update<T>(this SqlString s) where T : class {
            return s.Update().Append(Dry.Common.ActiveRecord.Mapping.Conventions.TableName<T>());
        }

        public static SqlString Set<T>(this SqlString s, Expression<Func<T, object>> exp) {
            return s.Append(" set ").Column(exp);
        }
    }
}
