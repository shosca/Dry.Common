using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using NHibernate;
using NHibernate.AdoNet.Util;
using NHibernate.Dialect;
using NHibernate.Engine;
using NHibernate.Id;
using NHibernate.Mapping.ByCode;
using NHibernate.SqlCommand;
using NHibernate.SqlTypes;
using NHibernate.Type;
using NHibernate.Util;

namespace Dry.Common.ActiveRecord {
    public class SingleTableHiLoGenerator : TransactionHelper, IPersistentIdentifierGenerator, IConfigurable {
        public static readonly IGeneratorDef Def = new GeneratorDef();

        public class GeneratorDef : IGeneratorDef {
            public string Class {
                get { return typeof (SingleTableHiLoGenerator).AssemblyQualifiedName; }
            }

            public object Params {
                get { return null; }
            }

            public Type DefaultReturnType {
                get { return typeof (int); }
            }

            public bool SupportedAsCollectionElementId {
                get { return true; }
            }
        }

        private static readonly IInternalLogger log = LoggerProvider.LoggerFor(typeof(TableGenerator));

        public const string WhereColumn = "wherecolumn";
        public const string WhereValue = "wherevalue";
        public const string ColumnParamName = "column";
        public const string TableParamName = "table";
        public const string MaxLo = "max_lo";

        public const string DefaultColumnName = "next_hi";
        public const string DefaultWhereColumnName = "tablename";
        public const string DefaultTableName = "nhibernate_hilo_table";

        string tableName;
        string columnName;
        string whereColumn;
        string whereValue;
        string query;
        long hi;
        long lo;
        long max_lo;
        Type returnClass;

        protected SqlType columnSqlType;
        protected SqlType wherecolumnSqlType;
        protected PrimitiveType columnType;

        SqlString updateSql;
        SqlType[] parameterTypes;

        static SortedSet<string> whereValues = new SortedSet<string>();

        public void Configure(IType type, IDictionary<string, string> parms, Dialect dialect) {
            tableName = PropertiesHelper.GetString(TableParamName, parms, DefaultTableName);
            columnName = PropertiesHelper.GetString(ColumnParamName, parms, DefaultColumnName);
            whereColumn = PropertiesHelper.GetString(WhereColumn, parms, DefaultWhereColumnName);
            whereValue = PropertiesHelper.GetString(WhereValue, parms, string.Empty);
            max_lo = PropertiesHelper.GetInt64(MaxLo, parms, Int16.MaxValue);
            lo = max_lo + 1;
            returnClass = type.ReturnedClass;

            if (string.IsNullOrEmpty(whereValue)) {
                log.Error("wherevalue for SingleTableHiLoGenerator is empty");
                throw new InvalidOperationException("wherevalue is empty");
            }

            whereValues.Add(whereValue);

            var schemaName = PropertiesHelper.GetString(PersistentIdGeneratorParmsNames.Schema, parms, null);
            var catalogName = PropertiesHelper.GetString(PersistentIdGeneratorParmsNames.Catalog, parms, null);

            if (tableName.IndexOf('.') < 0) {
                tableName = dialect.Qualify(catalogName, schemaName, tableName);
            }

            var selectBuilder = new SqlStringBuilder(100);
            selectBuilder.Add("select " + columnName)
                .Add(" from " + dialect.AppendLockHint(LockMode.Upgrade, tableName))
                .Add(" where ")
                .Add(whereColumn).Add("=").Add(whereValue);

            selectBuilder.Add(dialect.ForUpdateString);

            query = selectBuilder.ToString();

            columnType = type as PrimitiveType;
            if (columnType == null) {
                log.Error("Column type for TableGenerator is not a value type");
                throw new ArgumentException("type is not a ValueTypeType", "type");
            }

            // build the sql string for the Update since it uses parameters
            if (type is Int16Type) {
                columnSqlType = SqlTypeFactory.Int16;
            } else if (type is Int64Type) {
                columnSqlType = SqlTypeFactory.Int64;
            } else {
                columnSqlType = SqlTypeFactory.Int32;
            }
            wherecolumnSqlType = SqlTypeFactory.GetString(255);

            parameterTypes = new[] { columnSqlType, columnSqlType };

            var builder = new SqlStringBuilder(100);
            builder.Add("update " + tableName + " set ")
                .Add(columnName).Add("=").Add(Parameter.Placeholder)
                .Add(" where ")
                .Add(columnName).Add("=").Add(Parameter.Placeholder)
                .Add(" and ")
                .Add(whereColumn).Add("=").Add(whereValue);

            updateSql = builder.ToSqlString();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public object Generate(ISessionImplementor session, object obj) {
            if (max_lo < 1) {
                long val = Convert.ToInt64(DoWorkInNewTransaction(session));
                if (val == 0)
                    val = Convert.ToInt64(DoWorkInNewTransaction(session));
                return IdentifierGeneratorFactory.CreateNumber(val, returnClass);
            }
            if (lo > max_lo) {
                long hival = Convert.ToInt64(DoWorkInNewTransaction(session));
                lo = (hival == 0) ? 1 : 0;
                hi = hival*(max_lo + 1);
                log.Debug("New high value: " + hival);
            }

            return IdentifierGeneratorFactory.CreateNumber(hi + lo++, returnClass);
        }

        public string[] SqlCreateStrings(Dialect dialect) {
            var strings = new List<string> {
                "create table " + tableName +
                " ( " +
                whereColumn + " " + dialect.GetTypeName(wherecolumnSqlType) + ", " +
                columnName + " " + dialect.GetTypeName(columnSqlType) + ", " +
                " primary key (" + whereColumn + ")" +
                " )"
            };
            strings.AddRange(
                whereValues.Select(v =>
                    "insert into " + tableName + " (" + whereColumn + ", " + columnName + ") values ( " + v + ", 1 )"
                )
            );
            return strings.ToArray();
        }

        public string[] SqlDropString(Dialect dialect) {
            return new [] { dialect.GetDropTableString(tableName) };
        }

        public string GeneratorKey() {
            return tableName;
        }

        public override object DoWorkInCurrentTransaction(ISessionImplementor session, IDbConnection conn, IDbTransaction transaction) {
            long result;
            int rows;
            do {
                //the loop ensure atomicitiy of the
                //select + update even for no transaction
                //or read committed isolation level (needed for .net?)

                var qps = conn.CreateCommand();
                IDataReader rs = null;
                qps.CommandText = query;
                qps.CommandType = CommandType.Text;
                qps.Transaction = transaction;
                PersistentIdGeneratorParmsNames.SqlStatementLogger.LogCommand("Reading high value:", qps, FormatStyle.Basic);
                try {
                    rs = qps.ExecuteReader();
                    if (!rs.Read()) {
                        string err;
                        if (string.IsNullOrEmpty(whereColumn)) {
                            err = "could not read a hi value - you need to populate the table: " + tableName;
                        } else {
                            err = string.Format("could not read a hi value from table '{0}' using the wherecolumn({1}) and wherevalue({2})- you need to populate the table.", tableName, whereColumn, whereValue);
                        }
                        log.Error(err);
                        throw new IdentifierGenerationException(err);
                    }
                    result = Convert.ToInt64(columnType.Get(rs, 0));
                } catch (Exception e) {
                    log.Error("could not read a hi value", e);
                    throw;
                } finally {
                    if (rs != null) {
                        rs.Close();
                    }
                    qps.Dispose();
                }

                IDbCommand ups = session.Factory.ConnectionProvider.Driver.GenerateCommand(CommandType.Text, updateSql,
                                                                                           parameterTypes);
                ups.Connection = conn;
                ups.Transaction = transaction;

                try {
                    columnType.Set(ups, result + 1, 0);
                    columnType.Set(ups, result, 1);

                    PersistentIdGeneratorParmsNames.SqlStatementLogger.LogCommand("Updating high value:", ups, FormatStyle.Basic);

                    rows = ups.ExecuteNonQuery();
                } catch (Exception e) {
                    log.Error("could not update hi value in: " + tableName, e);
                    throw;
                } finally {
                    ups.Dispose();
                }
            }
            while (rows == 0);

            return result;
        }
    }
}
