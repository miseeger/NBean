﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using NBean.Enums;
using NBean.Interfaces;

namespace NBean
{
    internal class DatabaseAccess : IDatabaseAccess
    {
        private readonly DbConnection _connection;
        private readonly IDatabaseDetails _details;
        private readonly Stack<DbTransaction> _txStack = new Stack<DbTransaction>();
        private readonly Cache<DbCommandDescriptor, object> _cache = new Cache<DbCommandDescriptor, object>();

        public bool ImplicitTransactions { get; set; }
        public bool InTransaction => _txStack.Count > 0;
        public IsolationLevel TransactionIsolation { get; set; }
        public event Action<DbCommand> QueryExecuting;

        public int CacheCapacity
        {
            get => _cache.Capacity;
            set => _cache.Capacity = value;
        }

        public string Database => _connection.Database;
        public string Server => _connection.DataSource;
        public string ConnectionString => _connection.ConnectionString;
        public DatabaseType DbType => _details.DbType;


        public DatabaseAccess(DbConnection connection, IDatabaseDetails details)
        {
            _connection = connection;
            _details = details;
            ImplicitTransactions = true;
            TransactionIsolation = IsolationLevel.Unspecified;
        }


        public string GetDbTypeFromValue(object value)
        {
            return _details.GetSqlTypeFromRank(_details.GetRankFromValue(value));
        }


        public int Exec(string sql, params object[] parameters)
        {
            using (var cmd = CreateCommand(new DbCommandDescriptor(sql, parameters)))
            {
                QueryWillExecute(cmd);
                return cmd.ExecuteNonQuery();
            }
        }


        // ----- Iterators ----------------------------------------------------

        public IEnumerable<T> ColIterator<T>(string sql, params object[] parameters)
        {
            return EnumerateRecords(new DbCommandDescriptor(sql, parameters), GetFirstCellValue<T>);
        }


        public IEnumerable<IDictionary<string, object>> RowsIterator(string sql, params object[] parameters)
        {
            return EnumerateRecords(new DbCommandDescriptor(sql, parameters), RecordToDict);
        }


        // ----- Cell (aka Field) ---------------------------------------------

        public T Cell<T>(bool useCache, string sql, params object[] parameters)
        {
            return CacheableRead(true, true, useCache, sql, parameters, Cell<T>);
        }


        private T Cell<T>(DbCommandDescriptor descriptor)
        {
            return EnumerateRecords(descriptor, GetFirstCellValue<T>).FirstOrDefault();
        }



        // ----- Col ----------------------------------------------------------

        public T[] Col<T>(bool useCache, string sql, params object[] parameters)
        {
            return CacheableRead(true, false, useCache, sql, parameters, Col<T>);
        }


        private T[] Col<T>(DbCommandDescriptor descriptor)
        {
            return EnumerateRecords(descriptor, GetFirstCellValue<T>).ToArray();
        }


        // ----- Row ----------------------------------------------------------

        public IDictionary<string, object> Row(bool useCache, string sql, params object[] parameters)
        {
            return CacheableRead(false, true, useCache, sql, parameters, Row);
        }


        IDictionary<string, object> Row(DbCommandDescriptor descriptor)
        {
            return EnumerateRecords(descriptor, RecordToDict).FirstOrDefault();
        }


        // ----- Rows ---------------------------------------------------------

        public IDictionary<string, object>[] Rows(bool useCache, string sql, params object[] parameters)
        {
            return CacheableRead(false, false, useCache, sql, parameters, Rows);
        }


        IDictionary<string, object>[] Rows(DbCommandDescriptor descriptor)
        {
            return EnumerateRecords(descriptor, RecordToDict).ToArray();
        }


        // ----- Transactions -------------------------------------------------

        public void Transaction(Func<bool> action)
        {
            using (var tx = _connection.BeginTransaction(TransactionIsolation))
            {
                var shouldRollback = false;

                _txStack.Push(tx);

                try
                {
                    shouldRollback = !action();
                }
                catch
                {
                    shouldRollback = true;
                    throw;
                }
                finally
                {
                    if (shouldRollback)
                    {
                        _cache.Clear();
                        tx.Rollback();
                    }
                    else
                    {
                        tx.Commit();
                    }

                    _txStack.Pop();
                }
            }
        }


        // ----- Internals ----------------------------------------------------

        private DbCommand CreateCommand(DbCommandDescriptor descriptor)
        {
            var cmd = _connection.CreateCommand();
            var parameters = descriptor.Parameters;

            if (parameters.Length > 0)
            {
                var paramNames = new string[parameters.Length];
                // var paramValues = new object[parameters.Length]; // <--

                for (var i = 0; i < parameters.Length; i++)
                {
                    var name = _details.GetParamName(i);
                    paramNames[i] = name;
                    // paramValues[i] = parameters[i]; // <--

                    var p = cmd.CreateParameter();
                    p.ParameterName = name;
                    p.Value = parameters[i] ?? DBNull.Value;
                    cmd.Parameters.Add(p);
                }

                cmd.CommandText = string.Format(descriptor.Sql, paramNames);
                // cmd.CommandText = string.Format(descriptor.Sql, paramValues); // <--
            }
            else
            {
                cmd.CommandText = descriptor.Sql;
            }

            if (InTransaction)
                cmd.Transaction = _txStack.Peek();

            return cmd;
        }
        

        private IEnumerable<T> EnumerateRecords<T>(DbCommandDescriptor descriptor, Func<DbDataReader, T> converter)
        {
            using (var cmd = CreateCommand(descriptor))
            {
                QueryWillExecute(cmd);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        yield return converter(reader);
                }
            }
        }


        private static IDictionary<string, object> RecordToDict(DbDataReader reader)
        {
            var count = reader.FieldCount;
            var result = new Dictionary<string, object>();

            for (var i = 0; i < count; i++)
                result[reader.GetName(i)] = StripDbNull(reader.GetValue(i));

            return result;
        }


        private static T GetFirstCellValue<T>(DbDataReader reader)
        {
            return StripDbNull(reader.GetValue(0)).ConvertSafe<T>();
        }


        private static object StripDbNull(object value)
        {
            return value == DBNull.Value ? null : value;
        }

        private void QueryWillExecute(DbCommand cmd)
        {
            if (_cache.Count > 0 && !_details.IsReadOnlyCommand(cmd.CommandText))
                _cache.Clear();

            QueryExecuting?.Invoke(cmd);
        }

        private T CacheableRead<T>(bool singleCell, bool singleRow, bool useCache, string sql, object[] parameters, Func<DbCommandDescriptor, T> factory)
        {
            var descriptor = new DbCommandDescriptor((singleCell ? 1 : 0) + (singleRow ? 2 : 0), sql, parameters);

            if (useCache && _cache.Contains(descriptor))
                return (T)_cache.Get(descriptor);

            var fresh = factory(descriptor);

            if (useCache)
                _cache.Put(descriptor, fresh);
            else
                _cache.Remove(descriptor);

            return fresh;
        }
    }
}
