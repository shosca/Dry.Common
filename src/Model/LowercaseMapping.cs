using Castle.ActiveRecord;
using NHibernate.Mapping.ByCode;

namespace Dry.Common.Model {
    public class LowercaseMapping : IMappingContributor {
        public void Contribute(ModelMapper mapper) {
            mapper.BeforeMapClass += (i, m, map) => {
                map.Table(ActiveRecord.Mapping.Conventions.TableName(m).ToLowerInvariant());
                map.Lazy(true);
                map.Cache(c => c.Usage(CacheUsage.NonstrictReadWrite));
            };
            mapper.BeforeMapJoinedSubclass += (i, m, map) => {
                map.Table(ActiveRecord.Mapping.Conventions.TableName(m).ToLowerInvariant());
                map.Lazy(true);
            };
            mapper.BeforeMapUnionSubclass += (i, m, map) => {
                map.Table(ActiveRecord.Mapping.Conventions.TableName(m).ToLowerInvariant());
                map.Lazy(true);
            };

            mapper.AfterMapClass += (i, t, map) =>
                                    map.Id(x => x.Column("id".ToLowerInvariant()));

        }
    }
}
