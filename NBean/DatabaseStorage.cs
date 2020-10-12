using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NBean.Interfaces;

namespace NBean
{
    using Schema = Dictionary<string, IDictionary<string, int>>;

    internal class DatabaseStorage : IStorage, IValueRelaxations
    {
        private Schema _schema;
        private bool _isFluidMode;
        private readonly IDatabaseDetails _details;
        private readonly IDatabaseAccess _db;
        private readonly IKeyAccess _keyAccess;

        public bool TrimStrings { get; set; }
        public bool ConvertEmptyStringToNull { get; set; }
        public bool RecognizeIntegers { get; set; }


        public DatabaseStorage(IDatabaseDetails details, IDatabaseAccess db, IKeyAccess keys)
        {
            _details = details;
            _db = db;
            _keyAccess = keys;

            TrimStrings = true;
            ConvertEmptyStringToNull = true;
            RecognizeIntegers = true;

            _details.ExecInitCommands(_db);
        }


        public void EnterFluidMode()
        {
            _isFluidMode = true;
        }


        public void ExitFluidMode()
        {
            _isFluidMode = false;
        }


        public bool IsFluidMode()
        {
            return _isFluidMode;
        }


        internal Schema GetSchema()
        {
            return _schema ?? (_schema = LoadSchema());
        }


        internal void InvalidateSchema()
        {
            _schema = null;
        }


        private Schema LoadSchema()
        {
            var result = new Schema(StringComparer.CurrentCultureIgnoreCase);

            foreach (var tableName in _details.GetTableNames(_db))
            {
                var autoIncrementName = _keyAccess.GetAutoIncrementName(tableName);
                var columns = new Dictionary<string, int>();

                foreach (var col in _details.GetColumns(_db, tableName))
                {
                    var name = _details.GetColumnName(col);

                    if (name == autoIncrementName)
                        continue;

                    columns[name] = !_details.IsNullableColumn(col) || _details.GetColumnDefaultValue(col) != null
                        ? CommonDatabaseDetails.RANK_CUSTOM
                        : _details.GetRankFromSqlType(_details.GetColumnType(col));
                }

                result[tableName] = columns;
            }

            return result;
        }

        public bool IsKnownKind(string kind)
        {
            return GetSchema().ContainsKey(kind);
        }


        public bool IsNew(string kind, IDictionary<string, object> data)
        {
            var key = _keyAccess.GetKey(kind, data);
            var autoIncrement = _keyAccess.IsAutoIncrement(kind);

            if (!autoIncrement && key == null)
                throw new InvalidOperationException("Missing key value");

            return !autoIncrement ? !IsKnownKey(kind, key) : key == null;
        }


        public bool IsNew(Bean bean)
        {
            return IsNew(bean.GetKind(), bean.Export());
        }


        private IDictionary<string, int> GetColumnsFromData(IDictionary<string, object> data)
        {
            var result = new Dictionary<string, int>();

            foreach (var entry in data)
                result[entry.Key] = _details.GetRankFromValue(entry.Value);

            return result;
        }


        private object ConvertValue(object value)
        {
            switch (value)
            {
                case null:
                    return null;
                case ulong value1:
                    {
                        var number = value1;
                        value = number <= long.MaxValue ? (long)number : (decimal)number;
                        break;
                    }
            }

            switch (value)
            {
                case bool _ when _details.SupportsBoolean:
                    return value;
                case bool b:
                    value = b
                        ? 1
                        : 0;
                    break;
                case decimal _ when _details.SupportsDecimal:
                    return value;
                case decimal _:
                    value = Convert.ToString(value, CultureInfo.InvariantCulture);
                    break;
            }

            switch (value)
            {
                case int _:
                case long _:
                case byte _:
                case sbyte _:
                case short _:
                case ushort _:
                case uint _:
                case Enum _:
                    return _details.ConvertLongValue(Convert.ToInt64(value));
                case double _:
                case float _:
                    {
                        var number = Convert.ToDouble(value);
                        if (RecognizeIntegers && number.IsSafeInteger())
                            return _details.ConvertLongValue(Convert.ToInt64(number));

                        return number;
                    }
                case string s:
                    {
                        var text = s;

                        if (TrimStrings)
                            text = text.Trim();

                        if (ConvertEmptyStringToNull && text.Length < 1)
                            return null;

                        if (!RecognizeIntegers || text.Length <= 0 || text.Length >= 21 || char.IsLetter(text, 0))
                            return text;

                        if (!long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture,
                            out var number))
                            return text;

                        return number.ToString(CultureInfo.InvariantCulture) == text
                            ? _details.ConvertLongValue(number)
                            : text;
                    }
                default:
                    return value;
            }
        }


        public object Store(string kind, IDictionary<string, object> data, ICollection<string> dirtyNames = null)
        {
            var key = _keyAccess.GetKey(kind, data);
            var autoIncrement = _keyAccess.IsAutoIncrement(kind);

            var isNew = IsNew(kind, data);

            if (!isNew)
            {
                data = new Dictionary<string, object>(data);
                _keyAccess.SetKey(kind, data, null);
            }

            data = data
                .Where(e => dirtyNames == null || dirtyNames.Contains(e.Key))
                .ToDictionary(e => e.Key, e => ConvertValue(e.Value));

            if (_isFluidMode)
            {
                data = DropNulls(kind, data);
                CheckSchema(kind, data);
            }

            if (isNew)
            {
                var insertResult = _details.ExecInsert(_db, kind, _keyAccess.GetAutoIncrementName(kind), data);

                if (autoIncrement)
                    return insertResult;
            }
            else if (data.Count > 0)
            {
                ExecUpdate(kind, key, data);
            }

            return key;
        }


        public IDictionary<string, object> Load(string kind, object key)
        {
            if (_isFluidMode && !IsKnownKind(kind))
                return null;

            var (sql, parameters) = CreateSimpleByKeyArguments("SELECT *", kind, key);
            return _db.Row(true, sql, parameters);
        }


        public void Trash(string kind, object key)
        {
            if (_isFluidMode && !IsKnownKind(kind))
                return;

            var (sql, parameters) = CreateSimpleByKeyArguments("DELETE", kind, key);
            _db.Exec(sql, parameters);
        }


        private bool IsKnownKey(string kind, object key)
        {
            if (_isFluidMode && !IsKnownKind(kind))
                return false;

            var (sql, parameters) = CreateSimpleByKeyArguments("SELECT COUNT(*)", kind, key);

            return _db.Cell<int>(false, sql, parameters) > 0;
        }


        private void ExecUpdate(string kind, object key, IDictionary<string, object> data)
        {
            var propValues = new List<object>();
            var sql = new StringBuilder();

            sql
                .Append("UPDATE ")
                .Append(QuoteName(kind))
                .Append(" SET ");

            var index = 0;

            foreach (var entry in data)
            {
                if (index > 0)
                    sql.Append(", ");

                propValues.Add(entry.Value);

                sql
                    .Append(QuoteName(entry.Key))
                    .Append(" = ")
                    .Append("{").Append(index).Append("}");

                index++;
            }

            AppendKeyCriteria(kind, key, sql, propValues);

            if (_db.Exec(sql.ToString(), propValues.ToArray()) < 1)
                throw new Exception("Row not found");
        }


        private Tuple<string, object[]> CreateSimpleByKeyArguments(string prefix, string kind, object key)
        {
            var parameters = new List<object>();
            var sql = new StringBuilder(prefix)
                .Append(" FROM ")
                .Append(QuoteName(kind));

            AppendKeyCriteria(kind, key, sql, parameters);

            return Tuple.Create(sql.ToString(), parameters.ToArray());
        }


        private IDictionary<string, object> DropNulls(string kind, IDictionary<string, object> data)
        {
            var schema = GetSchema();
            var result = new Dictionary<string, object>();

            foreach (var entry in data)
            {
                if (entry.Value != null || schema.ContainsKey(kind) && schema[kind].ContainsKey(entry.Key))
                    result[entry.Key] = entry.Value;
            }

            return result;
        }

        private void CheckSchema(string kind, IDictionary<string, object> data)
        {
            var newColumns = GetColumnsFromData(data);
            var autoIncrementName = _keyAccess.GetAutoIncrementName(kind);

            if (autoIncrementName != null)
                newColumns.Remove(autoIncrementName);

            if (!IsKnownKind(kind))
            {

                foreach (var name in newColumns.Keys)
                    ValidateNewColumnRank(name, newColumns[name], data[name]);

                _db.Exec(CommonDatabaseDetails.FormatCreateTableCommand(_details, kind, autoIncrementName, newColumns));

                InvalidateSchema();
            }
            else
            {
                var oldColumns = GetSchema()[kind];
                var changedColumns = new Dictionary<string, int>();
                var addedColumns = new Dictionary<string, int>();

                foreach (var name in newColumns.Keys)
                {
                    var newRank = newColumns[name];

                    if (!oldColumns.ContainsKey(name))
                    {
                        ValidateNewColumnRank(name, newRank, data[name]);
                        addedColumns[name] = newRank;
                    }
                    else
                    {
                        var oldRank = oldColumns[name];

                        if (newRank > oldRank && Math.Max(oldRank, newRank) < CommonDatabaseDetails.RANK_STATIC_BASE)
                            changedColumns[name] = newRank;
                    }
                }

                if (changedColumns.Count <= 0 && addedColumns.Count <= 0)
                    return;

                _details.UpdateSchema(_db, kind, _keyAccess.GetAutoIncrementName(kind), oldColumns, changedColumns, addedColumns);
                InvalidateSchema();
            }
        }


        private static void ValidateNewColumnRank(string columnName, int rank, object value)
        {
            if (rank < CommonDatabaseDetails.RANK_CUSTOM)
                return;

            var text = $"Cannot automatically add column for property '{columnName}' of type '{value.GetType()}'";
            throw new InvalidOperationException(text);
        }


        private void AppendKeyCriteria(string kind, object key, StringBuilder sql, ICollection<object> parameters)
        {
            sql.Append(" WHERE ");

            var compound = key as CompoundKey;
            var names = _keyAccess.GetKeyNames(kind);

            if (names.Count > 1 ^ compound != null)
                throw new InvalidOperationException();

            var first = true;

            foreach (var name in names)
            {
                if (!first)
                    sql.Append(" AND ");

                sql.Append(QuoteName(name)).Append(" = {").Append(parameters.Count).Append("}");

                parameters.Add(compound != null
                    ? compound[name]
                    : key);

                first = false;
            }
        }


        private string QuoteName(string name)
        {
            return _details.QuoteName(name);
        }
    }
}
