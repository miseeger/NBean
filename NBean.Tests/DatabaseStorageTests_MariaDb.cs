#if !NO_MARIADB
using NBean.Tests.Fixtures;
using MySql.Data.MySqlClient;
using MySql.Data.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Xunit;

using NBean.Interfaces;
using NBean.Plugins;

namespace NBean.Tests {

    public class DatabaseStorageTests_MariaDb : IDisposable, IClassFixture<MariaDbConnectionFixture> {
        ConnectionFixture _fixture;
        IDatabaseAccess _db;
        KeyUtil _keys;
        DatabaseStorage _storage;
        BeanApi _api;

        public DatabaseStorageTests_MariaDb(MariaDbConnectionFixture fixture) {
            _fixture = fixture;
            _fixture.SetUpDatabase();

            var details = new MariaDbDetails();

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
            var details = new MariaDbDetails();

            _db.Exec(@"create table t(
                id int,

                ti1 TinyInt(123),
                ti2 TINYINT,
                ti3 bool,
                ti4 Boolean, 

                i1  integer(123),
                i2  Integer,
                i3  INT,

                bi1 bigint(123),
                bi2 BIGINT,

                d1  Double,
                d2  DOUBLE PRECISION,

                t1  varchar(8),
                t2  varchar(16),
                t3  varchar(32),
                t4  varchar(36),
                t5  varchar(64),
                t6  varchar(128),
                t7  varchar(190),
                t8  varchar(256),
                t9  varchar(512),
                t10 LongText,

                dt1 datetime,

                b1  longblob,

                x1  smallint,
                x2  mediumint,
                x3  double(3,2),
                x4  float,
                x5  decimal,
                x6  date,
                x7  timestamp,
                x8  char(36),
                x9  varchar(123),
                x10 binary,
                x11 blob,
                x12 text,

                x13 int unsigned,
                x14 int not null,
                x15 int default '123'
            )");

            var schema = _storage.GetSchema();
            Assert.Equal(1, schema.Count);

            var t = schema["t"];
            Assert.False(t.ContainsKey("id"));

            Assert.Equal(MariaDbDetails.RANK_INT8, t["ti1"]);
            Assert.Equal("TINYINT", details.GetSqlTypeFromRank(t["ti1"]));
            Assert.Equal(MariaDbDetails.RANK_INT8, t["ti2"]);
            Assert.Equal("TINYINT", details.GetSqlTypeFromRank(t["ti2"]));
            Assert.Equal(MariaDbDetails.RANK_INT8, t["ti3"]);
            Assert.Equal("TINYINT", details.GetSqlTypeFromRank(t["ti3"]));
            Assert.Equal(MariaDbDetails.RANK_INT8, t["ti4"]);
            Assert.Equal("TINYINT", details.GetSqlTypeFromRank(t["ti4"]));

            Assert.Equal(MariaDbDetails.RANK_INT32, t["i1"]);
            Assert.Equal("INT", details.GetSqlTypeFromRank(t["i1"]));
            Assert.Equal(MariaDbDetails.RANK_INT32, t["i2"]);
            Assert.Equal("INT", details.GetSqlTypeFromRank(t["i2"]));
            Assert.Equal(MariaDbDetails.RANK_INT32, t["i3"]);
            Assert.Equal("INT", details.GetSqlTypeFromRank(t["i3"]));

            Assert.Equal(MariaDbDetails.RANK_INT64, t["bi1"]);
            Assert.Equal("BIGINT", details.GetSqlTypeFromRank(t["bi1"]));
            Assert.Equal(MariaDbDetails.RANK_INT64, t["bi2"]);
            Assert.Equal("BIGINT", details.GetSqlTypeFromRank(t["bi2"]));

            Assert.Equal(MariaDbDetails.RANK_DOUBLE, t["d1"]);
            Assert.Equal("DOUBLE", details.GetSqlTypeFromRank(t["d1"]));
            Assert.Equal(MariaDbDetails.RANK_DOUBLE, t["d2"]);
            Assert.Equal("DOUBLE", details.GetSqlTypeFromRank(t["d2"]));

            Assert.Equal(MariaDbDetails.RANK_TEXT_8, t["t1"]);
            Assert.Equal("VARCHAR(8)", details.GetSqlTypeFromRank(t["t1"])); 
            Assert.Equal(MariaDbDetails.RANK_TEXT_16, t["t2"]);
            Assert.Equal("VARCHAR(16)", details.GetSqlTypeFromRank(t["t2"]));
            Assert.Equal(MariaDbDetails.RANK_TEXT_32, t["t3"]);
            Assert.Equal("VARCHAR(32)", details.GetSqlTypeFromRank(t["t3"]));
            Assert.Equal(MariaDbDetails.RANK_TEXT_36, t["t4"]);
            Assert.Equal("VARCHAR(36)", details.GetSqlTypeFromRank(t["t4"]));
            Assert.Equal(MariaDbDetails.RANK_TEXT_64, t["t5"]);
            Assert.Equal("VARCHAR(64)", details.GetSqlTypeFromRank(t["t5"]));
            Assert.Equal(MariaDbDetails.RANK_TEXT_128, t["t6"]);
            Assert.Equal("VARCHAR(128)", details.GetSqlTypeFromRank(t["t6"]));
            Assert.Equal(MariaDbDetails.RANK_TEXT_190, t["t7"]);
            Assert.Equal("VARCHAR(190)", details.GetSqlTypeFromRank(t["t7"]));
            Assert.Equal(MariaDbDetails.RANK_TEXT_256, t["t8"]);
            Assert.Equal("VARCHAR(256)", details.GetSqlTypeFromRank(t["t8"]));
            Assert.Equal(MariaDbDetails.RANK_TEXT_512, t["t9"]);
            Assert.Equal("VARCHAR(512)", details.GetSqlTypeFromRank(t["t9"]));
            Assert.Equal(MariaDbDetails.RANK_TEXT_MAX, t["t10"]);
            Assert.Equal("LONGTEXT", details.GetSqlTypeFromRank(t["t10"]));

            Assert.Equal(MariaDbDetails.RANK_STATIC_DATETIME, t["dt1"]);
            Assert.Equal("DATETIME", details.GetSqlTypeFromRank(t["dt1"]));

            Assert.Equal(MariaDbDetails.RANK_STATIC_BLOB, t["b1"]);
            Assert.Equal("LONGBLOB", details.GetSqlTypeFromRank(t["b1"]));

            foreach (var i in Enumerable.Range(1, 15))
                Assert.Equal(CommonDatabaseDetails.RANK_CUSTOM, t["x" + i]);
        }

        [Fact]
        public void CreateTable() {
            _storage.EnterFluidMode();

            var data = new Dictionary<string, object> {
                { "p1", null },
                { "p2", 1 },
                { "p3", 1000 },
                { "p4", Int64.MaxValue },
                { "p5", 3.14 },
                { "p6", "abcdfgh" },
                { "p7_16", "".PadRight(9, 'a') },
                { "p7_32", "".PadRight(17, 'a') },
                { "p7_36", "".PadRight(33, 'a') },
                { "p7_64", "".PadRight(37, 'a') },
                { "p7_128", "".PadRight(65, 'a') },
                { "p7_190", "".PadRight(129, 'a') },
                { "p7_256", "".PadRight(191, 'a') },
                { "p7_512", "".PadRight(257, 'a') },
                { "p8", "".PadRight(513, 'a') },
                { "p9", DateTime.Now },
                { "p10", new byte[0] }
            };

            _storage.Store("foo", data);

            var cols = _storage.GetSchema()["foo"];
            Assert.DoesNotContain("p1", cols.Keys);
            Assert.Equal(MariaDbDetails.RANK_INT8, cols["p2"]);
            Assert.Equal(MariaDbDetails.RANK_INT32, cols["p3"]);
            Assert.Equal(MariaDbDetails.RANK_INT64, cols["p4"]);
            Assert.Equal(MariaDbDetails.RANK_DOUBLE, cols["p5"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT_8, cols["p6"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT_16, cols["p7_16"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT_32, cols["p7_32"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT_36, cols["p7_36"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT_64, cols["p7_64"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT_128, cols["p7_128"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT_256, cols["p7_256"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT_512, cols["p7_512"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT_MAX, cols["p8"]);
            Assert.Equal(MariaDbDetails.RANK_STATIC_DATETIME, cols["p9"]);
            Assert.Equal(MariaDbDetails.RANK_STATIC_BLOB, cols["p10"]);
        }

        [Fact]
        public void AlterTable() {
            _storage.EnterFluidMode();

            var data = new Dictionary<string, object> {
                { "p1", 1 },
                { "p2", 1000 },
                { "p3", 1 + (long)Int32.MaxValue },
                { "p4", 3.14 },
                { "p5", "abc" },
                { "p6", "".PadRight(33, 'a') },
                { "p7", "".PadRight(65, 'a') },
                { "p8", "".PadRight(129, 'a') },
                { "p9", "".PadRight(513, 'a') }
            };

            _storage.Store("foo", data);

            for(var i = 1; i < data.Count; i++)
                data["p" + i] = data["p" + (i + 1)];

            data["p9"] = 123;
            data["p10"] = 123;

            _storage.Store("foo", data);

            var cols = _storage.GetSchema()["foo"];

            Assert.Equal(MariaDbDetails.RANK_INT32, cols["p1"]);
            Assert.Equal(MariaDbDetails.RANK_INT64, cols["p2"]);
            Assert.Equal(MariaDbDetails.RANK_DOUBLE, cols["p3"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT_8, cols["p4"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT_36, cols["p5"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT_128, cols["p6"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT_190, cols["p7"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT_MAX, cols["p8"]);
            Assert.Equal(MariaDbDetails.RANK_TEXT_MAX, cols["p9"]);
            Assert.Equal(MariaDbDetails.RANK_INT8, cols["p10"]);
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

                // supported ranks
                checker.Check(null, null);
                checker.Check((sbyte)123, (sbyte)123);
                checker.Check(1000, 1000);
                checker.Check(0x80000000L, 0x80000000L);
                checker.Check(3.14, 3.14);
                checker.Check("hello", "hello");
                checker.Check(SharedChecks.SAMPLE_DATETIME, SharedChecks.SAMPLE_DATETIME);
                checker.Check(SharedChecks.SAMPLE_BLOB, SharedChecks.SAMPLE_BLOB);

                // extremal vaues
                SharedChecks.CheckRoundtripOfExtremalValues(checker, checkDateTime: true);

                // conversion to string
                SharedChecks.CheckBigNumberRoundtripForcesString(checker);
                checker.Check(SharedChecks.SAMPLE_GUID, SharedChecks.SAMPLE_GUID.ToString());

                // bool            
                checker.Check(true, (sbyte)1);
                checker.Check(false, (sbyte)0);

                // enum
                checker.Check(DayOfWeek.Thursday, (sbyte)4);
            });
        }

        [Fact]
        public void Paginates()
        {
            var details = new MariaDbDetails();
            Assert.Equal("LIMIT 0, 10", details.Paginate(1));
            Assert.Equal("LIMIT 10, 10", details.Paginate(2));
            Assert.Equal("LIMIT 30, 15", details.Paginate(3, 15));
            Assert.Equal("LIMIT 0, 10", details.Paginate(-1));
        }

        [Fact]
        public void NotSupportedSqlRank()
        {
            var details = new MariaDbDetails();
            Assert.Throws<NotSupportedException>(() => details.GetSqlTypeFromRank(9999));
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
            _db.Exec("create table foo(id int, p geometry)");

            _db.QueryExecuting += cmd => {
                foreach(MySqlParameter p in cmd.Parameters) {
                    if(p.Value is MySqlGeometry)
                        p.MySqlDbType = MySqlDbType.Geometry;
                }
            };

            _storage.Store("foo", SharedChecks.MakeRow("p", new MySqlGeometry(54.2, 37.61667)));

            // http://stackoverflow.com/q/30584522
            var blob = _db.Cell<byte[]>(false, "select p from foo");
            Assert.Equal(54.2, new MySqlGeometry(MySqlDbType.Geometry, blob).XCoordinate);
        }

        [Fact]
        public void TransactionIsolation() {
            Assert.Equal(IsolationLevel.Unspecified, _db.TransactionIsolation);

            using(var otherFixture = new MariaDbConnectionFixture()) {
                var dbName = _db.Cell<string>(false, "select database()");
                var otherDb = new DatabaseAccess(otherFixture.Connection, null);

                otherDb.Exec("use " + dbName);
                SharedChecks.CheckReadUncommitted(_db, otherDb);
            }
        }

        // By default a MySQL server runs with the "latin" character-set and the
        // "latin1_swedisch_ci" collation. To make sure that this test runs correctly
        // the server must run with the "utfmb4" character-set and "utfmb4_unicode_ci"
        // collation. To override the default just start the server as follows:
        //     mysqld --character-set-server=utf8mb4 --collation-server=utf8mb4_unicode_ci

        [Fact]
        public void UTF8_mb4() {
            const string pile = "\U0001f4a9";
            _storage.EnterFluidMode();
            var id = _storage.Store("foo", SharedChecks.MakeRow("p", pile));
            Assert.Equal(pile, _storage.Load("foo", id)["p"]);
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
            _db.Exec("CREATE TABLE AUDIT (id INTEGER PRIMARY KEY)");
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
