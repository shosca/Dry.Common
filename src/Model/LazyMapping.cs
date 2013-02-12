using Castle.ActiveRecord;
using NHibernate.Mapping.ByCode;

namespace Dry.Common.Model {
    public class LazyMapping : IMappingContributor {
        public void Contribute(ModelMapper mapper) {
            mapper.BeforeMapBag += (i, m, map) => map.Lazy(CollectionLazy.Lazy);
            mapper.BeforeMapSet += (i, m, map) => map.Lazy(CollectionLazy.Lazy);
            mapper.BeforeMapList += (i, m, map) => map.Lazy(CollectionLazy.Lazy);
            mapper.BeforeMapManyToOne += (i, m, map) => map.Lazy(LazyRelation.Proxy);
            mapper.BeforeMapManyToMany += (i, m, map) => map.Lazy(LazyRelation.Proxy);
        }
    }
}