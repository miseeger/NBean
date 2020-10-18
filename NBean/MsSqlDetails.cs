#if !NO_MSSQL

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NBean.Enums;
using NBean.Interfaces;

namespace NBean
{
    internal class MsSqlDetails : IDatabaseDetails
    {
        public const int
            RANK_BYTE = 0,
            RANK_INT32 = 1,
            RANK_INT64 = 2,
            RANK_DOUBLE = 3,
            RANK_TEXT_8 = 4,
            RANK_TEXT_16 = 5,
            RANK_TEXT_32 = 6,
            RANK_TEXT_64 = 7,
            RANK_TEXT_128 = 8,
            RANK_TEXT_256 = 9,
            RANK_TEXT_512 = 10,
            RANK_TEXT_4000 = 11,
            RANK_TEXT_MAX = 12,
            RANK_STATIC_DATETIME = CommonDatabaseDetails.RANK_STATIC_BASE + 1,
            RANK_STATIC_DATETIME_OFFSET = CommonDatabaseDetails.RANK_STATIC_BASE + 2,
            RANK_STATIC_GUID = CommonDatabaseDetails.RANK_STATIC_BASE + 3,
            RANK_STATIC_BLOB = CommonDatabaseDetails.RANK_STATIC_BASE + 4;

        public DatabaseType DbType => DatabaseType.MsSql;

        public string AutoIncrementSqlType => "BIGINT IDENTITY(1,1) PRIMARY KEY";

        public bool SupportsBoolean => false;

        public bool SupportsDecimal => false;


        public string GetParamName(int index)
        {
            return $"@p{index}";
        }


        public string QuoteName(string name)
        {
            if (name.Contains("]"))
                throw new ArgumentException();

            return $"[{name}]";
        }


        public void ExecInitCommands(IDatabaseAccess db) { }


        public object ExecInsert(IDatabaseAccess db, string tableName, string autoIncrementName, 
            IDictionary<string, object> data)
        {
            var hasAutoIncrement = !string.IsNullOrEmpty(autoIncrementName);

            var valuesPrefix = hasAutoIncrement
                ? "output inserted." + QuoteName(autoIncrementName)
                : null;

            var sql = CommonDatabaseDetails.FormatInsertCommand(this, tableName, data.Keys, valuesPrefix: valuesPrefix);
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
                case byte _:
                    return RANK_BYTE;
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
                        
                        if (len <= 64)
                            return RANK_TEXT_64;

                        if (len <= 128)
                            return RANK_TEXT_128;

                        if (len <= 256)
                            return RANK_TEXT_256;

                        if (len <= 512)
                            return RANK_TEXT_512;

                        return len <= 4000
                            ? RANK_TEXT_4000
                            : RANK_TEXT_MAX;
                    }
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
            if (sqlType.StartsWith("48:"))
                return RANK_BYTE;

            if (sqlType.StartsWith("56:"))
                return RANK_INT32;

            if (sqlType.StartsWith("127:"))
                return RANK_INT64;

            if (sqlType.StartsWith("42:"))
                return RANK_STATIC_DATETIME;

            if (sqlType.StartsWith("43:"))
                return RANK_STATIC_DATETIME_OFFSET;

            if (sqlType.StartsWith("36:"))
                return RANK_STATIC_GUID;

            switch (sqlType)
            {
                case "62:8":
                    return RANK_DOUBLE;

                case "231:16":
                    return RANK_TEXT_8;

                case "231:32":
                    return RANK_TEXT_16;

                case "231:64":
                    return RANK_TEXT_32;

                case "231:128":
                    return RANK_TEXT_64;

                case "231:256":
                    return RANK_TEXT_128;

                case "231:512":
                    return RANK_TEXT_256;

                case "231:1024":
                    return RANK_TEXT_512;

                case "231:8000":
                    return RANK_TEXT_4000;

                case "231:-1":
                    return RANK_TEXT_MAX;

                case "165:-1":
                    return RANK_STATIC_BLOB;
            }

            return CommonDatabaseDetails.RANK_CUSTOM;
        }


        public string GetSqlTypeFromRank(int rank)
        {
            switch (rank)
            {
                case RANK_BYTE:
                    return "TINYINT";

                case RANK_INT32:
                    return "INT";

                case RANK_INT64:
                    return "BIGINT";

                case RANK_DOUBLE:
                    return "FLOAT(53)";

                case RANK_TEXT_8:
                    return "NVARCHAR(8)";

                case RANK_TEXT_16:
                    return "NVARCHAR(16)";

                case RANK_TEXT_32:
                    return "NVARCHAR(32)";

                case RANK_TEXT_64:
                    return "NVARCHAR(64)";

                case RANK_TEXT_128:
                    return "NVARCHAR(128)";

                case RANK_TEXT_256:
                    return "NVARCHAR(256)";

                case RANK_TEXT_512:
                    return "NVARCHAR(512)";

                case RANK_TEXT_4000:
                    return "NVARCHAR(4000)";

                case RANK_TEXT_MAX:
                    return "NVARCHAR(MAX)";

                case RANK_STATIC_DATETIME:
                    return "DATETIME2";

                case RANK_STATIC_DATETIME_OFFSET:
                    return "DATETIMEOFFSET";

                case RANK_STATIC_GUID:
                    return "UNIQUEIDENTIFIER";

                case RANK_STATIC_BLOB:
                    return "VARBINARY(MAX)";
            }

            throw new NotSupportedException();
        }


        public object ConvertLongValue(long value)
        {
            if (value.IsUnsignedByteRange())
                return (byte)value;

            if (value.IsInt32Range())
                return (int)value;

            return value;
        }


        public IEnumerable<string> GetTableNames(IDatabaseAccess db)
        {
            return db.Col<string>(false, 
                "SELECT name FROM sys.objects WHERE type='U'");
        }


        public IEnumerable<IDictionary<string, object>> GetColumns(IDatabaseAccess db, string tableName)
        {
            return db.Rows(false, 
                "SELECT name, system_type_id, max_length, is_nullable, object_definition(default_object_id) [default] " +
                "FROM sys.columns WHERE object_id = OBJECT_ID({0})", tableName);
        }


        public bool IsNullableColumn(IDictionary<string, object> column)
        {
            return (bool)column["is_nullable"];
        }


        public object GetColumnDefaultValue(IDictionary<string, object> column)
        {
            return column["default"];
        }


        public string GetColumnName(IDictionary<string, object> column)
        {
            return (string)column["name"];
        }


        public string GetColumnType(IDictionary<string, object> column)
        {
            return string.Concat(column["system_type_id"], ":", column["max_length"]);
        }


        public void UpdateSchema(IDatabaseAccess db, string tableName, string autoIncrementName, 
            IDictionary<string, int> oldColumns, IDictionary<string, int> changedColumns, 
            IDictionary<string, int> addedColumns)
        {
            CommonDatabaseDetails.FixLongToDoubleUpgrade(this, db, tableName, oldColumns, changedColumns, 
                RANK_INT64, RANK_DOUBLE, RANK_TEXT_32);

            tableName = QuoteName(tableName);

            foreach (var entry in changedColumns)
                db.Exec(
                    $"ALTER TABLE {tableName} ALTER COLUMN {QuoteName(entry.Key)} {GetSqlTypeFromRank(entry.Value)}");

            foreach (var entry in addedColumns)
                db.Exec($"ALTER TABLE {tableName} ADD {QuoteName(entry.Key)} {GetSqlTypeFromRank(entry.Value)}");
        }


        public bool IsReadOnlyCommand(string text)
        {
            return Regex.IsMatch(text, @"^\s*SELECT\W", RegexOptions.IgnoreCase);
        }
    }
}
#endif
