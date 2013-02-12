#region using

using System;
using NHibernate.Persister.Entity;

#endregion

namespace Dry.Common.ActiveRecord {
    public static class NHEventHelper {
        public static void Set(IEntityPersister persister, object[] state, string propertyName, object value) {
            var index = Array.IndexOf(persister.PropertyNames, propertyName);
            if (index == -1)
                return;
            state[index] = value;
        }
    }
}
