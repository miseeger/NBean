#if !NO_MSSQL
using NBean.Tests.Fixtures;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using Xunit;

using NBean.Interfaces;
using NBean.Plugins;

namespace NBean.Tests {

    [Trait("db", "mssql")]
    public class DatabaseStorageTests_MsSql : IDisposable, IClassFixture<MsSqlConnectionFixture> {
        ConnectionFixture _fixture;
        IDatabaseAccess _db;
        KeyUtil _keys;
        DatabaseStorage _storage;
        BeanApi _api;

        public DatabaseStorageTests_MsSql(MsSqlConnectionFixture fixture) {
            _fixture = fixture;
            _fixture.SetUpDatabase();

            var details = new MsSqlDetails();

            _db = new DatabaseAccess(_fixture.Connection, details);
            _keys = new KeyUtil();
            _storage = new DatabaseStorage(details, _db, _keys);
            _api = new BeanApi(_fixture.Connection);
        }

        public void Dispose() {
            _fixture.TearDownDatabase();
        }

        [Fact]
        public void Schema() {
            _db.Exec(@"create table foo(
                id  int,

                b   tinyint,
                i   int,
                l   bigint,
                d   float(53),
                t1  nvarchar(8),
                t2  nvarchar(16),
                t3  nvarchar(32),
                t4  nvarchar(64),
                t5  nvarchar(128),
                t6  nvarchar(256),
                t7  nvarchar(512),
                t8  nvarchar(4000),
                t9  nvarchar(MAX),
                dt  datetime2,
                dto datetimeoffset,
                g   uniqueidentifier,
                bl  varbinary(MAX),

                x1  bit,
                x2  decimal,
                x3  float(10),
                x4  varchar(32),
                x5  nvarchar(33),
                x6  datetime,
                x7  varbinary(123),

                x8  int not null,
                x9  int default 123
            )");

            var schema = _storage.GetSchema();
            Assert.Equal(1, schema.Count);

            var cols = schema["foo"];
            Assert.False(cols.ContainsKey("id"));

            Assert.Equal(MsSqlDetails.RANK_BYTE, cols["b"]);
            Assert.Equal(MsSqlDetails.RANK_INT32, cols["i"]);
            Assert.Equal(MsSqlDetails.RANK_INT64, cols["l"]);
            Assert.Equal(MsSqlDetails.RANK_DOUBLE, cols["d"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_8, cols["t1"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_16, cols["t2"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_32, cols["t3"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_64, cols["t4"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_128, cols["t5"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_256, cols["t6"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_512, cols["t7"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_4000, cols["t8"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_MAX, cols["t9"]);
            Assert.Equal(MsSqlDetails.RANK_STATIC_DATETIME, cols["dt"]);
            Assert.Equal(MsSqlDetails.RANK_STATIC_DATETIME_OFFSET, cols["dto"]);
            Assert.Equal(MsSqlDetails.RANK_STATIC_GUID, cols["g"]);
            Assert.Equal(MsSqlDetails.RANK_STATIC_BLOB, cols["bl"]);

            foreach(var i in Enumerable.Range(1, 9))
                Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, cols["x" + i]);
        }

        [Fact]
        public void CreateTable() {
            _storage.EnterFluidMode();

            var data = new Dictionary<string, object> {
                { "p1", null },
                { "p2", 1 },
                { "p3", -1 },
                { "p4", Int64.MaxValue },
                { "p5", 3.14 },
                { "p6", "abcdfgh" },
                { "p7_16", "".PadRight(9, 'a') },
                { "p7_32", "".PadRight(17, 'a') },
                { "p7_64", "".PadRight(33, 'a') },
                { "p7_128", "".PadRight(65, 'a') },
                { "p7_256", "".PadRight(129, 'a') },
                { "p7_512", "".PadRight(257, 'a') },
                { "p7_4k", "".PadRight(513, 'a') },
                { "p8", "".PadRight(4001, 'a') },
                { "p9", DateTime.Now },
                { "p10", DateTimeOffset.Now },
                { "p11", Guid.NewGuid() },
                { "p12", new byte[0] }
            };

            _storage.Store("foo", data);

            var cols = _storage.GetSchema()["foo"];
            Assert.DoesNotContain("p1", cols.Keys);
            Assert.Equal(MsSqlDetails.RANK_BYTE, cols["p2"]);
            Assert.Equal(MsSqlDetails.RANK_INT32, cols["p3"]);
            Assert.Equal(MsSqlDetails.RANK_INT64, cols["p4"]);
            Assert.Equal(MsSqlDetails.RANK_DOUBLE, cols["p5"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_8, cols["p6"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_16, cols["p7_16"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_32, cols["p7_32"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_64, cols["p7_64"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_128, cols["p7_128"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_256, cols["p7_256"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_512, cols["p7_512"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_4000, cols["p7_4k"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_MAX, cols["p8"]);
            Assert.Equal(MsSqlDetails.RANK_STATIC_DATETIME, cols["p9"]);
            Assert.Equal(MsSqlDetails.RANK_STATIC_DATETIME_OFFSET, cols["p10"]);
            Assert.Equal(MsSqlDetails.RANK_STATIC_GUID, cols["p11"]);
            Assert.Equal(MsSqlDetails.RANK_STATIC_BLOB, cols["p12"]);
        }

        [Fact]
        public void AlterTable() {
            _storage.EnterFluidMode();

            var data = new Dictionary<string, object> {
                { "p1", 1 },
                { "p2", -1 },
                { "p3", 1 + (long)Int32.MaxValue },
                { "p4", 3.14 },
                { "p5", "abc" },
                { "p6", "".PadRight(17, 'a') },
                { "p7", "".PadRight(65, 'a') },
                { "p8", "".PadRight(513, 'a') },
                { "p9", "".PadRight(4001, 'a') }
            };

            _storage.Store("foo", data);

            for(var i = 1; i < 9; i++)
                data["p" + i] = data["p" + (i + 1)];

            data["p9"] = 123;
            data["p10"] = 123;

            _storage.Store("foo", data);

            var cols = _storage.GetSchema()["foo"];
            Assert.Equal(MsSqlDetails.RANK_INT32, cols["p1"]);
            Assert.Equal(MsSqlDetails.RANK_INT64, cols["p2"]);
            Assert.Equal(MsSqlDetails.RANK_DOUBLE, cols["p3"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_8, cols["p4"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_32, cols["p5"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_128, cols["p6"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_4000, cols["p7"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_MAX, cols["p8"]);
            Assert.Equal(MsSqlDetails.RANK_TEXT_MAX, cols["p9"]);
            Assert.Equal(MsSqlDetails.RANK_BYTE, cols["p10"]);
        }

        [Fact]
        public void LongToDouble() {
            SharedChecks.CheckLongToDouble(_db, _storage);
        }

        [Fact]
        public void Roundtrip() {
            AssertExtensions.WithCulture("de-DE", () => 
            {
                _storage.EnterFluidMode();
                var checker = new RoundtripChecker(_db, _storage);

                // similar to https://github.com/StackExchange/dapper-dot-net/issues/229
                _db.QueryExecuting += cmd => {
                    foreach(SqlParameter p in cmd.Parameters) {
                        if(Equals(p.Value, DateTime.MinValue))
                            p.SqlDbType = SqlDbType.DateTime2;
                    }
                };

                // supported ranks
                checker.Check(null, null);
                checker.Check(255, (byte)255);
                checker.Check(1000, 1000);
                checker.Check(0x80000000L, 0x80000000L);
                checker.Check(3.14, 3.14);
                checker.Check("hello", "hello");
                checker.Check(SharedChecks.SAMPLE_DATETIME, SharedChecks.SAMPLE_DATETIME);
                checker.Check(SharedChecks.SAMPLE_DATETIME_OFFSET, SharedChecks.SAMPLE_DATETIME_OFFSET);
                checker.Check(SharedChecks.SAMPLE_GUID, SharedChecks.SAMPLE_GUID);
                checker.Check(SharedChecks.SAMPLE_BLOB, SharedChecks.SAMPLE_BLOB);

                // extremal vaues
                SharedChecks.CheckRoundtripOfExtremalValues(checker, checkDateTime: true, checkDateTimeOffset: true);

                // conversion to string
                SharedChecks.CheckBigNumberRoundtripForcesString(checker);

                // bool            
                checker.Check(true, (byte)1);
                checker.Check(false, (byte)0);

                // enum
                checker.Check(DayOfWeek.Thursday, (byte)4);
            });
        }

        [Fact]
        public void SchemaReadingKeepsCache() {
            SharedChecks.CheckSchemaReadingKeepsCache(_db, _storage);
        }

        [Fact]
        public void DateTimeQueries() {
            SharedChecks.CheckDateTimeQueries(_db, _storage);
        }

        [Fact]
        public void GuidQuery() {
            SharedChecks.CheckGuidQuery(_db, _storage);
        }

        [Fact]
        public void CompoundKey() {
            SharedChecks.CheckCompoundKey(_storage, _keys);
        }

        [Fact]
        public void StoringNull() {
            SharedChecks.CheckStoringNull(_storage);
        }

        [Fact]
        public void CustomRank_MissingColumn() {
            SharedChecks.CheckCustomRank_MissingColumn(_db, _storage);
        }

        [Fact]
        public void CustomRank_ExistingColumn() {
            _db.Exec("create table foo(id int, p smallmoney)");                       
            _storage.Store("foo", SharedChecks.MakeRow("p", new SqlMoney(9.9)));
            Assert.Equal(9.900M, _db.Cell<object>(false, "select p from foo"));
        }

        [Fact]
        public void TransactionIsolation() {
            Assert.Equal(IsolationLevel.Unspecified, _db.TransactionIsolation);

            using(var otherFixture = new MsSqlConnectionFixture()) {
                var dbName = _db.Cell<string>(false, "select db_name()");
                var otherDb = new DatabaseAccess(otherFixture.Connection, null);

                otherDb.Exec("use " + dbName);
                try {
                    SharedChecks.CheckReadUncommitted(_db, otherDb);
                } finally {
                    otherDb.Exec("use master");
                }
            }
        }

        [Fact]
        public void AuditTableIsCreated()
        {
            _api.AddObserver(new Auditor(_api, string.Empty));
            Assert.Equal(1, _api.Count(false, "AUDIT"));
            Assert.True(_storage.IsKnownKind("AUDIT"));
        }

        [Fact]
        public void AuditTableAlreadyExists()
        {
            _db.Exec("CREATE TABLE Audit (id INTEGER PRIMARY KEY)");
            _api.AddObserver(new Auditor(_api, string.Empty));
            Assert.Equal(0, _api.Count(false, "AUDIT"));
            Assert.True(_storage.IsKnownKind("AUDIT"));
            _db.Exec("DROP TABLE Audit");
        }

        [Fact]
        public void IsNewBean()
        {
            _storage.EnterFluidMode();

            _storage.Store("foo", SharedChecks.MakeRow("a", 1));

            var foo = _api.Dispense("foo");
            foo["a"] = 2;

            Assert.True(_storage.IsNew(foo));
        }

        [Fact]
        public void NotIsNewBean()
        {
            _storage.EnterFluidMode();

            var key = _storage.Store("foo", SharedChecks.MakeRow("a", 1));
            var foo = _api.Load("foo", key);

            Assert.False(_storage.IsNew(foo));
        }
    }
}
#endif
