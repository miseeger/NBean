using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NBean.Interfaces;

namespace NBean
{
    internal static class CommonDatabaseDetails
    {
        public const int
            RANK_NULL = int.MinValue,
            RANK_STATIC_BASE = 100,
            RANK_CUSTOM = int.MaxValue;


        public static string QuoteWithBackticks(string text)
        {
            if (text.Contains("`"))
                throw new ArgumentException();

            return "`" + text + "`";
        }


        public static string FormatCreateTableCommand(IDatabaseDetails details, string tableName, string autoIncrementName, ICollection<KeyValuePair<string, int>> columns)
        {
            var sql = new StringBuilder()
                .Append("CREATE TABLE ")
                .Append(details.QuoteName(tableName))
                .Append(" (");

            var colSpecs = new List<string>(1 + columns.Count);

            if (!string.IsNullOrEmpty(autoIncrementName))
                colSpecs
                    .Add($"{details.QuoteName(autoIncrementName)} {details.AutoIncrementSqlType}");

            colSpecs
                .AddRange(columns.Select(pair => details.QuoteName(pair.Key) + " "
                    + details.GetSqlTypeFromRank(pair.Value)));

            sql
                .Append(string.Join(", ", colSpecs))
                .Append(")");

            var postfix = details.GetCreateTableStatementPostfix();

            if (!string.IsNullOrEmpty(postfix))
                sql.Append(" ").Append(postfix);

            return sql.ToString();
        }


        public static string FormatInsertCommand(IDatabaseDetails details, string tableName, ICollection<string> fieldNames, string valuesPrefix = null, string defaultsExpr = "default values", string postfix = null)
        {
            var builder = new StringBuilder("INSERT INTO ")
                .Append(details.QuoteName(tableName))
                .Append(" ");

            if (fieldNames.Count > 0)
            {
                builder
                    .Append("(")
                    .Append(string.Join(", ", fieldNames.Select(details.QuoteName)))
                    .Append(") ");
            }

            if (!string.IsNullOrEmpty(valuesPrefix))
                builder.Append(valuesPrefix).Append(" ");

            if (fieldNames.Count > 0)
            {
                builder.Append("values (");

                for (var i = 0; i < fieldNames.Count; i++)
                {
                    if (i > 0)
                        builder.Append(", ");

                    builder.Append("{").Append(i).Append("}");
                }

                builder.Append(")");
            }
            else
            {
                builder.Append(defaultsExpr);
            }

            if (!string.IsNullOrEmpty(postfix))
                builder.Append(" ").Append(postfix);

            return builder.ToString();
        }

        public static void FixLongToDoubleUpgrade(IDatabaseDetails details, IDatabaseAccess db, string tableName, IDictionary<string, int> oldColumns, IDictionary<string, int> changedColumns, int longRank, int doubleRank, int safeRank)
        {
            var names = new List<string>(changedColumns.Keys);
            var quotedTableName = details.QuoteName(tableName);

            foreach (var name in names)
            {
                var oldRank = oldColumns[name];
                var newRank = changedColumns[name];

                if (oldRank != longRank || newRank != doubleRank)
                    continue;

                var quotedName = details.QuoteName(name);
                var min = db.Cell<long>(false, "SELECT MIN(" + quotedName + ") FROM " + quotedTableName);
                var max = db.Cell<long>(false, "SELECT MAX(" + quotedName + ") FROM " + quotedTableName);

                if (!min.IsInt53Range() || !max.IsInt53Range())
                    changedColumns[name] = safeRank;
            }
        }
    }
}
