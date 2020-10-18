#if !NO_PGSQL

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NBean.Enums;
using NBean.Interfaces;

namespace NBean
{
    class PgSqlDetails : IDatabaseDetails
    {
        public const int
            RANK_BOOLEAN = 0,
            RANK_INT32 = 1,
            RANK_INT64 = 2,
            RANK_DOUBLE = 3,
            RANK_NUMERIC = 4,
            RANK_TEXT = 5,
            RANK_STATIC_DATETIME = CommonDatabaseDetails.RANK_STATIC_BASE + 1,
            RANK_STATIC_DATETIME_OFFSET = CommonDatabaseDetails.RANK_STATIC_BASE + 2,
            RANK_STATIC_GUID = CommonDatabaseDetails.RANK_STATIC_BASE + 3,
            RANK_STATIC_BLOB = CommonDatabaseDetails.RANK_STATIC_BASE + 4;

        public DatabaseType DbType => DatabaseType.PgSql;

        public string AutoIncrementSqlType => "BIGSERIAL";

        public bool SupportsBoolean => true;

        public bool SupportsDecimal => true;


        public string GetParamName(int index)
        {
            return $":p{index}";
        }


        public string QuoteName(string name)
        {
            return $"{'"'}{name}{'"'}";
        }


        public void ExecInitCommands(IDatabaseAccess db)
        {
            // set names?
        }

        public object ExecInsert(IDatabaseAccess db, string tableName, string autoIncrementName, 
            IDictionary<string, object> data)
        {
            var hasAutoIncrement = !string.IsNullOrEmpty(autoIncrementName);

            var postfix = hasAutoIncrement
                ? "returning " + QuoteName(autoIncrementName)
                : null;

            var sql = CommonDatabaseDetails.FormatInsertCommand(this, tableName, data.Keys, postfix: postfix);
            var values = data.Values.ToArray();

            if (hasAutoIncrement)
                return db.Cell<object>(false, sql, values);

            db.Exec(sql, values);

            return null;
        }


        public string GetCreateTableStatementPostfix()
        {
            return null;
        }


        public int GetRankFromValue(object value)
        {
            switch (value)
            {
                case null:
                    return CommonDatabaseDetails.RANK_NULL;
                case bool _:
                    return RANK_BOOLEAN;
                case int _:
                    return RANK_INT32;
                case long _:
                    return RANK_INT64;
                case double _:
                    return RANK_DOUBLE;
                case decimal _:
                    return RANK_NUMERIC;
                case string _:
                    return RANK_TEXT;
                case DateTime _:
                    return RANK_STATIC_DATETIME;
                case DateTimeOffset _:
                    return RANK_STATIC_DATETIME_OFFSET;
                case Guid _:
                    return RANK_STATIC_GUID;
                case byte[] _:
                    return RANK_STATIC_BLOB;
                default:
                    return CommonDatabaseDetails.RANK_CUSTOM;
            }
        }


        public int GetRankFromSqlType(string sqlType)
        {
            sqlType = sqlType.ToUpper();

            switch (sqlType)
            {
                case "BOOLEAN":
                    return RANK_BOOLEAN;

                case "INTEGER 32":
                    return RANK_INT32;

                case "BIGINT 64":
                    return RANK_INT64;

                case "DOUBLE PRECISION 53":
                    return RANK_DOUBLE;

                case "NUMERIC":
                    return RANK_NUMERIC;

                case "TEXT":
                    return RANK_TEXT;

                case "TIMESTAMP WITHOUT TIME ZONE":
                    return RANK_STATIC_DATETIME;

                case "TIMESTAMP WITH TIME ZONE":
                    return RANK_STATIC_DATETIME_OFFSET;

                case "UUID":
                    return RANK_STATIC_GUID;

                case "BYTEA":
                    return RANK_STATIC_BLOB;
            }

            return CommonDatabaseDetails.RANK_CUSTOM;
        }


        public string GetSqlTypeFromRank(int rank)
        {
            switch (rank)
            {
                case RANK_BOOLEAN:
                    return "BOOLEAN";

                case RANK_INT32:
                    return "INTEGER";

                case RANK_INT64:
                    return "BIGINT";

                case RANK_DOUBLE:
                    return "DOUBLE PRECISION";

                case RANK_NUMERIC:
                    return "NUMERIC";

                case RANK_TEXT:
                    return "TEXT";

                case RANK_STATIC_DATETIME:
                    return "TIMESTAMP WITHOUT TIME ZONE";

                case RANK_STATIC_DATETIME_OFFSET:
                    return "TIMESTAMP WITH TIME ZONE";

                case RANK_STATIC_GUID:
                    return "UUID";

                case RANK_STATIC_BLOB:
                    return "BYTEA";
            }

            throw new NotSupportedException();
        }


        public object ConvertLongValue(long value)
        {
            if (value.IsInt32Range())
                return (int)value;

            return value;
        }


        public IEnumerable<string> GetTableNames(IDatabaseAccess db)
        {
            return db.Col<string>(false, 
                "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'");
        }


        public IEnumerable<IDictionary<string, object>> GetColumns(IDatabaseAccess db, string tableName)
        {
            return db.Rows(false, 
                "SELECT * FROM information_schema.columns WHERE table_name = {0}", tableName);
        }


        public bool IsNullableColumn(IDictionary<string, object> column)
        {
            return "YES".Equals(column["is_nullable"]);
        }


        public object GetColumnDefaultValue(IDictionary<string, object> column)
        {
            return column["column_default"];
        }


        public string GetColumnName(IDictionary<string, object> column)
        {
            return (string)column["column_name"];
        }


        public string GetColumnType(IDictionary<string, object> column)
        {
            var type = (string)column["data_type"];
            var prec = column["numeric_precision"];
            if (prec != null)
                type += " " + prec;

            return type;
        }


        public void UpdateSchema(IDatabaseAccess db, string tableName, string autoIncrementName, 
            IDictionary<string, int> oldColumns, IDictionary<string, int> changedColumns, 
            IDictionary<string, int> addedColumns)
        {
            var operations = new List<string>();

            CommonDatabaseDetails.FixLongToDoubleUpgrade(this, db, tableName, oldColumns, changedColumns, 
                RANK_INT64, RANK_DOUBLE, RANK_NUMERIC);

            foreach (var entry in changedColumns)
                operations.Add(
                    $"ALTER {QuoteName(entry.Key)} TYPE {GetSqlTypeFromRank(entry.Value)} USING {QuoteName(entry.Key)}::{GetSqlTypeFromRank(entry.Value)}");

            foreach (var entry in addedColumns)
                operations.Add($"ADD {QuoteName(entry.Key)} {GetSqlTypeFromRank(entry.Value)}");

            db.Exec("alter table " + QuoteName(tableName) + " " + string.Join(", ", operations));
        }


        public bool IsReadOnlyCommand(string text)
        {
            return Regex.IsMatch(text, @"^\s*SELECT\W", RegexOptions.IgnoreCase);
        }
    }
}
#endif
