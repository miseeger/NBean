#if !NO_MARIADB
using MySql.Data.MySqlClient;

namespace NBean.Tests.Fixtures {

    public class MariaDbConnectionFixture : ConnectionFixture {
        string _dbName;

        public static string ConnectionString {
            get { return $"server={Host}; uid={User}; pwd={Password}; charset=utf8mb4"; }
        }

        static string Host {
            get { return GetEnvVar("MARIA_HOST", "localhost"); }
        }

        static string User {
            get { return GetEnvVar("MARIA_USER", "root"); }
        }

        static string Password {
            get { return GetEnvVar("MARIA_PWD", ""); }
        }

        public MariaDbConnectionFixture() {
            _dbName = GenerateTempDbName();

            Connection = new MySqlConnection(ConnectionString);
            Connection.Open();
        }

        public override void Dispose() {
            Connection.Close();
        }

        public override void SetUpDatabase() {
            Exec(Connection, "set sql_mode=STRICT_TRANS_TABLES");
            Exec(Connection, "create database " + _dbName);
            Exec(Connection, "use " + _dbName);
        }

        public override void TearDownDatabase() {
            Exec(Connection, "drop database if exists " + _dbName);
        }
    }

}
#endif
