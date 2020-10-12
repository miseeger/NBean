#if !NO_MSSQL
using System.Collections.Generic;
using System.Data.SqlClient;

namespace NBean.Tests.Fixtures {

    public class MsSqlConnectionFixture : ConnectionFixture {
        ICollection<string> _dropList = new List<string>();

        public static string ConnectionString {
            get { return "server=" + ServerName + "; user instance=true; integrated security=true; connection timeout=90"; }
        }

        static string ServerName {
            get { return GetEnvVar("MSSQL_NAME", ".\\SQLEXPRESS"); }
        }

        public MsSqlConnectionFixture() {
            Connection = new SqlConnection(ConnectionString);
            Connection.Open();
        }

        public override void Dispose() {
            Exec(Connection, "USE master");
            foreach(var name in _dropList)
                Exec(Connection, "DROP DATABASE " + name);

            Connection.Close();
        }

        public override void SetUpDatabase() {
            var name = GenerateTempDbName();
            _dropList.Add(name);

            Exec(Connection, "CREATE DATABASE " + name);
            Exec(Connection, "USE " + name);  
        }

        public override void TearDownDatabase() {
        }
    }

}
#endif
