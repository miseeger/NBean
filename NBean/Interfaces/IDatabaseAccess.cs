using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Dynamic;
using NBean.Enums;

namespace NBean.Interfaces
{
    using Row = IDictionary<string, object>;

    public interface IDatabaseAccess : ITransactionSupport
    {
        event Action<DbCommand> QueryExecuting;

        int CacheCapacity { get; set; }
        string Database { get; }
        string Server { get; }
        string ConnectionString { get; }
        DatabaseType DbType { get; }

        string GetDbTypeFromValue(object value);
 
        int Exec(string sql, params object[] parameters);

        IEnumerable<T> ColIterator<T>(string sql, params object[] parameters);
        IEnumerable<Row> RowsIterator(string sql, params object[] parameters);

        T Cell<T>(bool useCache, string sql, params object[] parameters);
        T[] Col<T>(bool useCache, string sql, params object[] parameters);
        Row Row(bool useCache, string sql, params object[] parameters);
        Row[] Rows(bool useCache, string sql, params object[] parameters);
    }
}
