#region using

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Dry.Common;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using Dry.Common.ActiveRecord.Model;

#endregion

namespace Dry.Common.ActiveRecord.Mapping {
    public static class Conventions {
        public static ModelMapper ClassMapHiLo<TEntity>(this ModelMapper mapper, Action<IClassMapper<TEntity>> mapaction) where TEntity : BaseHiLoModel<TEntity> {
            mapper.AddMapping(new ClassMapping<TEntity>());
            mapper.Class<TEntity>(map => {
                map.IdHilo();
                mapaction(map);
            });
            return mapper;
        }

        public static ModelMapper ClassMapGuid<TEntity>(this ModelMapper mapper, Action<IClassMapper<TEntity>> mapaction) where TEntity : BaseGuidModel<TEntity> {
            mapper.AddMapping(new ClassMapping<TEntity>());
            mapper.Class<TEntity>(map => {
                map.IdGuidComb();
                mapaction(map);
            });
            return mapper;
        }

        public static void IdHilo<TEntity>(this IClassMapper<TEntity> map) where TEntity : BaseHiLoModel<TEntity> {
            map.IdHilo(100);
        }

        public static void IdHilo<TEntity>(this IClassMapper<TEntity> map, int maxlo) where TEntity : BaseHiLoModel<TEntity> {
            map.Id(
                x => x.Id,
                m => m.Generator(SingleTableHiLoGenerator.Def,
                    g => g.Params(
                        new {
                            max_lo = maxlo,
                            wherevalue = "'" + TableName<TEntity>() + "'"
                        }
                    )
                )
            );
        }

        public static void IdGuidComb<TEntity>(this IClassMapper<TEntity> map) where TEntity : BaseGuidModel<TEntity> {
            map.Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        }

        public static string Table<T>(this IClassMapper<T> map) where T : class {
            var tbname = TableName<T>();
            map.Table(tbname);
            return tbname;
        }

        public static string Table<T>(this IUnionSubclassMapper<T> map) where T : class {
            var tbname = TableName<T>();
            map.Table(tbname);
            return tbname;
        }

        public static string TablePrefix<T>() where T : class {
            return TablePrefix(typeof(T));
        }

        public static string TablePrefix(Type type) {
            var prefixattr = type.Assembly.GetAttr<TablePrefixAttribute>();
            var prefix = prefixattr != null ? prefixattr.Prefix + "_" : string.Empty;
            return prefix.ToLowerInvariant();
        }

        public static string TableName<T>() where T : class {
            return TableName(typeof(T));
        }
        public static string TableName(Type t) {
            var name = TablePrefix(t) + t.Name;
            return name.ToLowerInvariant();
        }
    }
}
