using System;
using System.Data.Common;

namespace NBean
{
    internal abstract class ConnectionContainer
    {
        public abstract DbConnection Connection { get; }

        public virtual void Dispose() { }


        internal class SimpleImpl : ConnectionContainer
        {
            public SimpleImpl(DbConnection conn)
            {
                Connection = conn;
            }

            public override DbConnection Connection { get; }
        }


        internal class LazyImpl : ConnectionContainer
        {
            DbConnection _conn;
            string _connectionString;
            Func<DbConnection> _factory;

            public LazyImpl(string connectionString, Func<DbConnection> factory)
            {
                _connectionString = connectionString;
                _factory = factory;
            }

            public override DbConnection Connection
            {
                get
                {
                    if (_conn != null) 
                        return _conn;

                    _conn = _factory();
                    _conn.ConnectionString = _connectionString;
                    _conn.Open();
                    _factory = null;
                    _connectionString = null;

                    return _conn;
                }
            }

            public sealed override void Dispose()
            {
                _conn?.Dispose();
            }
        }
    }
}
