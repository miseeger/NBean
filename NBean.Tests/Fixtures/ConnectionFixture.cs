using System;
using System.Data.Common;

namespace NBean.Tests.Fixtures {

    public abstract class ConnectionFixture : IDisposable {
        public DbConnection Connection { get; set; }

        public abstract void Dispose();
        public abstract void SetUpDatabase();
        public abstract void TearDownDatabase();

        protected static string GenerateTempDbName() {
            return "nbean_" + Guid.NewGuid().ToString("N");
        }

        protected static string GetEnvVar(string key, string defaultValue) {
            return Environment.GetEnvironmentVariable("NBEAN_TEST_" + key) ?? defaultValue;
        }

        protected static void Exec(DbConnection conn, string sql) {
            using(var cmd = conn.CreateCommand()) {
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }
    }

}
