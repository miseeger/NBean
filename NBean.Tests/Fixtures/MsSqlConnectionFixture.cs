﻿
using System.IO;
#if !NO_MSSQL
using System.Data.SqlClient;

namespace NBean.Tests.Fixtures {

    public class MsSqlConnectionFixture : ConnectionFixture {
        string _dbName;

        string MdfPath => GetDbFilePath(".mdf");
        string LdfPath => GetDbFilePath(".ldf");

        string GetDbFilePath(string ext) => Path.Combine(Path.GetTempPath(), _dbName + ext);

        public MsSqlConnectionFixture() {
            Connection = new SqlConnection("server=(localdb)\\MSSQLLocalDB; connection timeout=90");
            Connection.Open();
        }

        public override void Dispose() {
            Connection.Close();
        }

        public override void SetUpDatabase() {
            _dbName = GenerateTempDbName();

            Exec(Connection, $@"create database {_dbName} on (name={_dbName}, 
                filename='{MdfPath}') log on (name={_dbName}_log, filename='{LdfPath}')");
            Exec(Connection, "use " + _dbName);
        }

        public override void TearDownDatabase()
        {
            Exec(Connection, "use master");
            Exec(Connection, "sp_detach_db " + _dbName);
            File.Delete(MdfPath);
            File.Delete(LdfPath);
        }
    }

}
#endif
