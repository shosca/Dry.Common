using System.Collections;
using System.Reflection;
using Castle.ActiveRecord;
using NHibernate.Mapping.ByCode;

namespace Dry.Common.Model {
    public class ComponentPrefixMapping : IMappingContributor {
        public void Contribute(ModelMapper mapper) {
            mapper.BeforeMapProperty += (i, m, map) => {
                var prop = m.LocalMember as PropertyInfo;
                if (prop != null && i.IsComponent(prop.DeclaringType) && !typeof(IEnumerable).IsAssignableFrom(m.PreviousPath.LocalMember.GetPropertyOrFieldType()))
                    map.Column(m.PreviousPath.LocalMember.Name + m.LocalMember.Name);
            };
            mapper.BeforeMapManyToOne += (i, m, map) => {
                var prop = m.LocalMember as PropertyInfo;
                if (prop != null && i.IsComponent(prop.DeclaringType) && !typeof(IEnumerable).IsAssignableFrom(m.PreviousPath.LocalMember.GetPropertyOrFieldType()))
                    map.Column(m.PreviousPath.LocalMember.Name + m.LocalMember.Name);
            };
        }
    }
}
