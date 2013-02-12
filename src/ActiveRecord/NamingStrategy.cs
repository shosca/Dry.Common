#region using

using Castle.ActiveRecord;
using NHibernate.Cfg;
using NHibernate.Util;

#endregion

namespace Dry.Common.ActiveRecord {
    public class NamingStrategy : INamingStrategy {
        public string ClassToTableName(string className) {
            return StringHelper.Unqualify(className).ToLowerInvariant();
        }

        public string PropertyToColumnName(string propertyName) {
            return StringHelper.Unqualify(propertyName).ToLowerInvariant();
        }

        public string TableName(string tableName) {
            return tableName.ToLowerInvariant();
        }

        public string ColumnName(string columnName) {
            return columnName.ToLowerInvariant();
        }

        public string PropertyToTableName(string className, string propertyName) {
            return StringHelper.Unqualify(propertyName).ToLowerInvariant();
        }

        public string LogicalColumnName(string columnName, string propertyName) {
            return StringHelper.IsNotEmpty(columnName)
                ? columnName.ToLowerInvariant() :
                StringHelper.Unqualify(propertyName).ToLowerInvariant();
        }
    }

    public class NamingStrategyContributor : INHContributor {
        public static INamingStrategy strategy = new NamingStrategy();

        public void Contribute(Configuration cfg) {
            cfg.SetNamingStrategy(strategy);
        }
    }
}
