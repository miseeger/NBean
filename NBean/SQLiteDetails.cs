using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NBean.Enums;
using NBean.Interfaces;

namespace NBean  {
    internal class SQLiteDetails : IDatabaseDetails {

        public const int RANK_ANY = 0;

        public DatabaseType DbType => DatabaseType.Sqlite;

        public string AutoIncrementSqlType => "INTEGER PRIMARY KEY";

        public bool SupportsBoolean => false;

        public bool SupportsDecimal => false;


        public string GetParamName(int index) {
            return $":p{index}";
        }


        public string QuoteName(string name) {
            return CommonDatabaseDetails.QuoteWithBackticks(name);
        }


        public string Paginate(int page, int perPage = 10)
        {
            return $"LIMIT {perPage} OFFSET {((page < 1 ? 1 : page) - 1) * perPage}";
        }


        public void ExecInitCommands(IDatabaseAccess db) {            
        }


        public object ExecInsert(IDatabaseAccess db, string tableName, string autoIncrementName, IDictionary<string, object> data) {
            db.Exec(
                CommonDatabaseDetails.FormatInsertCommand(this, tableName, data.Keys),
                data.Values.ToArray()
            );

            if (string.IsNullOrEmpty(autoIncrementName))
                return null;

            // per-connection, robust to triggers
            // http://www.sqlite.org/c3ref/last_insert_rowid.html

            return db.Cell<long>(false, "SELECT LAST_INSERT_ROWID()");
        }


        public string GetCreateTableStatementPostfix() {
            return null;
        }


        public int GetRankFromValue(object value)
        {
            return value == null 
                ? CommonDatabaseDetails.RANK_NULL 
                : RANK_ANY;
        }


        public int GetRankFromSqlType(string sqlType) {
            return RANK_ANY;
        }


        public string GetSqlTypeFromRank(int rank) {
            return null;
        }


        public object ConvertLongValue(long value) {
            return value;
        }


        public IEnumerable<string> GetTableNames(IDatabaseAccess db) {
            return db.Col<string>(false, "SELECT name FROM sqlite_master WHERE type = 'table' AND name <> 'sqlite_sequence'");
        }


        public IEnumerable<IDictionary<string, object>> GetColumns(IDatabaseAccess db, string tableName) {
            return db.Rows(false, "PRAGMA TABLE_INFO(" + QuoteName(tableName) + ")");
        }


        public bool IsNullableColumn(IDictionary<string, object> column) {
            return true;
        }


        public object GetColumnDefaultValue(IDictionary<string, object> column) {
            return null;
        }


        public string GetColumnName(IDictionary<string, object> column) {
            return (string)column["name"];
        }


        public string GetColumnType(IDictionary<string, object> column) {
            return null;
        }


        public void UpdateSchema(IDatabaseAccess db, string tableName, string autoIncrementName, IDictionary<string, int> oldColumns, IDictionary<string, int> changedColumns, IDictionary<string, int> addedColumns) {
            if (changedColumns.Count > 0)
                throw new NotSupportedException();

            foreach(var entry in addedColumns)
                db.Exec(
                    $"ALTER TABLE {QuoteName(tableName)} ADD {QuoteName(entry.Key)} {GetSqlTypeFromRank(entry.Value)}");
        }


        public bool IsReadOnlyCommand(string text) {
            return Regex.IsMatch(text, @"^\s*(SELECT|PRAGMA TABLE_INFO)", RegexOptions.IgnoreCase);            
        }
    }
}
