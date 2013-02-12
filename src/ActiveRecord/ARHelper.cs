#region using

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Scopes;
using Castle.Core.Configuration;
using Castle.Core.Logging;
using Castle.MonoRail.Framework;
using Dry.Common;
using NHibernate;
using NHibernate.Metadata;
using NHibernate.Persister.Collection;
using NHibernate.Type;

#endregion

namespace Dry.Common.ActiveRecord {
    public static class ARHelper {
        public static IDbTransaction GetUnderlyingTransaction(this ISession session) {
            using (var cmd = session.Connection.CreateCommand()) {
                if (session.Transaction != null && session.Transaction.IsActive)
                    session.Transaction.Enlist(cmd);

                return cmd.Transaction;
            }
        }

        public static IDbTransaction GetUnderlyingTransaction(this IStatelessSession session) {
            using (var cmd = session.Connection.CreateCommand()) {
                if (session.Transaction != null && session.Transaction.IsActive)
                    session.Transaction.Enlist(cmd);

                return cmd.Transaction;
            }
        }

        public static void CreateSchema(IEngineContext context) {
            AR.CreateSchema();
            AR.ConfigurationSource.GetAllConfigurationKeys().ForAll(key => {
                var config = AR.ConfigurationSource.GetConfiguration(key);
                config.Assemblies.ForAll(a => {
                    var initializers = a.GetTypes().Where(t => !t.IsInterface && typeof (IDefaultFixtureProvider).IsAssignableFrom(t));
                    initializers.ForAll(i => {
                        var initializer = Activator.CreateInstance(i) as IDefaultFixtureProvider;
                        if (initializer == null) return;
                        using (var transaction = new TransactionScope()) {
                            try {
                                initializer.CreateDefaultFixture();
                                transaction.VoteCommit();
                            }
                            catch {
                                transaction.VoteRollBack();
                                throw;
                            }
                        }
                    });

                });
            });
        }

        public static void DropSchema() {
            AR.DropSchema();
        }

        public static void GenerateCreationScripts(string path) {
            AR.GenerateCreationScripts(path);
        }

        public static PropertyInfo GetPk(this IClassMetadata model) {
            return model.GetMappedClass(EntityMode.Poco).GetProperty(model.IdentifierPropertyName);
        }
    }
}
