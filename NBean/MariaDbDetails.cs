#if !NO_MARIADB

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NBean.Enums;
using NBean.Interfaces;

namespace NBean
{
    internal class MariaDbDetails : IDatabaseDetails
    {
        public const int
            RANK_INT8 = 0,
            RANK_INT32 = 1,
            RANK_INT64 = 2,
            RANK_DOUBLE = 3,
            RANK_TEXT_8 = 4,
            RANK_TEXT_16 = 5,
            RANK_TEXT_32 = 6,
            
            // up to Guid len
            RANK_TEXT_36 = 7,
            RANK_TEXT_64 = 8,
            RANK_TEXT_128 = 9,
            
            // The 191 character limit is due to the maximum key length of 767 bytes.
            // For a 4 byte character, this means a max of 191 characters(floor(767/4) = 191).
            RANK_TEXT_190 = 10,
            RANK_TEXT_256 = 11,
            RANK_TEXT_512 = 12,
            RANK_TEXT_MAX = 13,

            RANK_STATIC_DATETIME = CommonDatabaseDetails.RANK_STATIC_BASE + 1,
            RANK_STATIC_BLOB = CommonDatabaseDetails.RANK_STATIC_BASE + 2;

        private string _charset;

        public DatabaseType DbType => DatabaseType.MariaDb;

        public string AutoIncrementSqlType => "BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY";

        public bool SupportsBoolean => false;

        public bool SupportsDecimal => false;


        public string GetParamName(int index)
        {
            return $"@p{index}";
        }


        public string QuoteName(string name)
        {
            return CommonDatabaseDetails.QuoteWithBackticks(name);
        }


        public string Paginate(int page, int perPage = 10)
        {
            return $"LIMIT {((page < 1 ? 1 : page) - 1) * perPage}, {perPage}";
        }


        public void ExecInitCommands(IDatabaseAccess db)
        {
            _charset = db.Cell<string>(false, "SHOW CHARSET LIKE 'utf8mb4'") != null
                ? "utf8mb4"
                : "utf8";

            db.Exec("SET NAMES " + _charset);
        }


        public object ExecInsert(IDatabaseAccess db, string tableName, string autoIncrementName, 
            IDictionary<string, object> data)
        {
            db.Exec(
                CommonDatabaseDetails.FormatInsertCommand(this, tableName, data.Keys, defaultsExpr: "VALUES ()"),
                data.Values.ToArray()
            );

            return string.IsNullOrEmpty(autoIncrementName)
                ? null
                : db.Cell<object>(false, "SELECT LAST_INSERT_ID()");

            // per-connection, http://stackoverflow.com/q/21185666
            // robust to triggers, http://dba.stackexchange.com/a/25141
        }


        public string GetCreateTableStatementPostfix()
        {
            return $"engine=InnoDB default charset={_charset} collate={_charset}_unicode_ci";
        }


        public int GetRankFromValue(object value)
        {
            switch (value)
            {
                case null:
                    return CommonDatabaseDetails.RANK_NULL;
                case sbyte _:
                    return RANK_INT8;
                case int _:
                    return RANK_INT32;
                case long _:
                    return RANK_INT64;
                case double _:
                    return RANK_DOUBLE;
                case string s:
                    {
                        var len = s.Length;

                        if (len <= 8)
                            return RANK_TEXT_8;

                        if (len <= 16)
                            return RANK_TEXT_16;

                        if (len <= 32)
                            return RANK_TEXT_32;

                        if (len <= 36)
                            return RANK_TEXT_36;

                        if (len <= 64)
                            return RANK_TEXT_64;

                        if (len <= 128)
                            return RANK_TEXT_128;

                        if (len <= 190)
                            return RANK_TEXT_190;

                        if (len <= 256)
                            return RANK_TEXT_256;

                        return len <= 512
                            ? RANK_TEXT_512
                            : RANK_TEXT_MAX;
                    }
                case DateTime _:
                    return RANK_STATIC_DATETIME;
                case Guid _:
                    return RANK_TEXT_36;
                case byte[] _:
                    return RANK_STATIC_BLOB;
                default:
                    return CommonDatabaseDetails.RANK_CUSTOM;
            }
        }


        public int GetRankFromSqlType(string sqlType)
        {
            sqlType = sqlType.ToUpper();

            if (sqlType.Contains("UNSIGNED"))
                return CommonDatabaseDetails.RANK_CUSTOM;

            if (sqlType.StartsWith("TINYINT("))
                return RANK_INT8;

            if (sqlType.StartsWith("INT("))
                return RANK_INT32;

            if (sqlType.StartsWith("BIGINT("))
                return RANK_INT64;

            switch (sqlType.ToUpper())
            {
                case "DOUBLE":
                    return RANK_DOUBLE;
                case "VARCHAR(8)":
                    return RANK_TEXT_8;
                case "VARCHAR(16)":
                    return RANK_TEXT_16;
                case "VARCHAR(32)":
                    return RANK_TEXT_32;
                case "VARCHAR(36)":
                    return RANK_TEXT_36;
                case "VARCHAR(64)":
                    return RANK_TEXT_64;
                case "VARCHAR(128)":
                    return RANK_TEXT_128;
                case "VARCHAR(190)":
                    return RANK_TEXT_190;
                case "VARCHAR(256)":
                    return RANK_TEXT_256;
                case "VARCHAR(512)":
                    return RANK_TEXT_512;
                case "LONGTEXT":
                    return RANK_TEXT_MAX;
                case "DATETIME":
                    return RANK_STATIC_DATETIME;
                default:
                    return sqlType == "LONGBLOB"
                        ? RANK_STATIC_BLOB
                        : CommonDatabaseDetails.RANK_CUSTOM;
            }
        }


        // TEXT types in InnoDB
        // 
        // Type       | Maximum length
        // -----------+-------------------------------------
        // TINYTEXT   |           255 (2 8−1) bytes
        // TEXT       |        65,535 (216−1) bytes = 64 KiB
        // MEDIUMTEXT |    16,777,215 (224−1) bytes = 16 MiB
        // LONGTEXT   | 4,294,967,295 (232−1) bytes =  4 GiB
        public string GetSqlTypeFromRank(int rank)
        {
            switch (rank)
            {
                case RANK_INT8:
                    return "TINYINT";

                case RANK_INT32:
                    return "INT";

                case RANK_INT64:
                    return "BIGINT";

                case RANK_DOUBLE:
                    return "DOUBLE";

                case RANK_TEXT_8:
                    return "VARCHAR(8)";

                case RANK_TEXT_16:
                    return "VARCHAR(16)";

                case RANK_TEXT_32:
                    return "VARCHAR(32)";

                case RANK_TEXT_36:
                    return "VARCHAR(36)";

                case RANK_TEXT_64:
                    return "VARCHAR(64)";

                case RANK_TEXT_128:
                    return "VARCHAR(128)";

                case RANK_TEXT_190:
                    return "VARCHAR(190)";

                case RANK_TEXT_256:
                    return "VARCHAR(256)";

                case RANK_TEXT_512:
                    return "VARCHAR(512)";

                case RANK_TEXT_MAX:
                    return "LONGTEXT";

                case RANK_STATIC_DATETIME:
                    return "DATETIME";

                case RANK_STATIC_BLOB:
                    return "LONGBLOB";
            }

            throw new NotSupportedException();
        }


        public object ConvertLongValue(long value)
        {
            if (value.IsSignedByteRange())
                return (sbyte)value;

            if (value.IsInt32Range())
                return (int)value;

            return value;
        }


        public IEnumerable<string> GetTableNames(IDatabaseAccess db)
        {

            return db.Col<string>(false, "SHOW TABLES");
        }


        public IEnumerable<IDictionary<string, object>> GetColumns(IDatabaseAccess db, string tableName)
        {
            return db.Rows(false, "SHOW COLUMNS FROM " + QuoteName(tableName));
        }


        public bool IsNullableColumn(IDictionary<string, object> column)
        {
            return "YES".Equals(column["Null"]);
        }


        public object GetColumnDefaultValue(IDictionary<string, object> column)
        {
            return column["Default"];
        }


        public string GetColumnName(IDictionary<string, object> column)
        {
            return (string)column["Field"];
        }


        public string GetColumnType(IDictionary<string, object> column)
        {
            return (string)column["Type"];
        }


        public void UpdateSchema(IDatabaseAccess db, string tableName, string autoIncrementName, 
            IDictionary<string, int> oldColumns, IDictionary<string, int> changedColumns, 
            IDictionary<string, int> addedColumns)
        {
            CommonDatabaseDetails.FixLongToDoubleUpgrade(this, db, tableName, oldColumns, changedColumns, 
                RANK_INT64, RANK_DOUBLE, RANK_TEXT_36);

            var operations = changedColumns
                .Select(entry =>
                    $"CHANGE {QuoteName(entry.Key)} {QuoteName(entry.Key)} {GetSqlTypeFromRank(entry.Value)}")
                .ToList();

            operations.AddRange(addedColumns.Select(entry =>
                $"ADD {QuoteName(entry.Key)} {GetSqlTypeFromRank(entry.Value)}"));

            db.Exec("ALTER TABLE " + QuoteName(tableName) + " " + string.Join(", ", operations));
        }


        public bool IsReadOnlyCommand(string text)
        {
            return Regex.IsMatch(text, @"^\s*(SELECT|SHOW)\W", RegexOptions.IgnoreCase);
        }
    }
}
#endif
