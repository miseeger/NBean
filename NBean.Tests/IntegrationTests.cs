using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NBean.Enums;
using Xunit;

using NBean.Interfaces;
using NBean.Plugins;

namespace NBean.Tests
{

    public class IntegrationTests
    {
        [Fact]
        public void GettersAndSetters()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                api.CurrentUser = "TestBot";
                Assert.Equal("TestBot", api.CurrentUser);

                Assert.NotNull(api.BeanOptions);

                api.CacheCapacity = 5;
                Assert.Equal(5, api.CacheCapacity);

                Assert.NotNull(api.Server);
                Assert.NotEmpty(api.ConnectionString);

                api.ImplicitTransactions = true;
                Assert.True(api.ImplicitTransactions);

                Assert.False(api.InTransaction);

                api.TransactionIsolation = IsolationLevel.RepeatableRead;
                Assert.Equal(IsolationLevel.RepeatableRead, api.TransactionIsolation);

                api.TrimStrings = true;
                Assert.True(api.TrimStrings);

                api.ConvertEmptyStringToNull = true;
                Assert.True(api.ConvertEmptyStringToNull);

                api.RecognizeIntegers = true;
                Assert.True(api.RecognizeIntegers);
            }
        }

        [Fact]
        public void SetQueryExecutingHandler()
        {
            // Only for "touching" the code ;-)
            using (var api = SQLitePortability.CreateApi())
            {
                api.QueryExecuting += cmd =>
                {
                    Debug.WriteLine("Query Executing");
                };

                api.QueryExecuting -= cmd =>
                {
                    Debug.WriteLine("Query Executing");
                };

                Assert.True(true);
            }
        }

        [Fact]
        public void ImplicitTransactionsOnStoreAndTrash()
        {
            using (var conn = SQLitePortability.CreateConnection())
            {
                conn.Open();

                IDatabaseDetails details = new SQLiteDetails();
                IDatabaseAccess db = new DatabaseAccess(conn, details);
                IKeyAccess keys = new KeyUtil();
                DatabaseStorage storage = new DatabaseStorage(details, db, keys);
                IBeanFactory factory = new BeanFactory();
                IBeanCrud crud = new BeanCrud(storage, db, keys, factory);

                storage.EnterFluidMode();

                var bean = crud.Dispense<ThrowingBean>();
                bean["foo"] = "ok";
                var id = crud.Store(bean);

                bean.Throw = true;
                bean["foo"] = "fail";

                try { crud.Store(bean); } catch { }
                Assert.Equal("ok", db.Cell<string>(true, "select foo from test where id = {0}", id));

                try { crud.Trash(bean); } catch { }
                Assert.True(db.Cell<int>(true, "select count(*) from test") > 0);
            }
        }

        [Fact]
        public void DisableImplicitTransactions()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                api.EnterFluidMode();
                api.ImplicitTransactions = false;

                var bean = api.Dispense<ThrowingBean>();
                bean.Throw = true;
                try { api.Store(bean); } catch { }

                Assert.Equal(1, api.Count<ThrowingBean>());
            }
        }

        [Fact]
        public void Api_DetailsSelection()
        {
            var mariaBeanApi = new BeanApi(new MySql.Data.MySqlClient.MySqlConnection());

            Assert.Equal(DatabaseType.Sqlite, new BeanApi(SQLitePortability.CreateConnection()).CreateDetails().DbType);

#if !NO_MSSQL
            Assert.Equal(DatabaseType.MsSql, new BeanApi(new System.Data.SqlClient.SqlConnection()).CreateDetails().DbType);
#endif

#if !NO_MARIADB
            Assert.Equal(DatabaseType.MariaDb, new BeanApi(new MySql.Data.MySqlClient.MySqlConnection()).CreateDetails().DbType);
#endif

#if !NO_PGSQL
            Assert.Equal(DatabaseType.PgSql, new BeanApi(new Npgsql.NpgsqlConnection()).CreateDetails().DbType);
#endif
        }

        [Fact]
        public void CreateApiWithProviderFactory()
        {
            Assert.Equal(DatabaseType.Sqlite, new BeanApi("data source=:memory:",
                new SQLiteFactory()).CreateDetails().DbType);
        }

        [Fact]
        public void CreateApiWithInitialObservers()
        {
            BeanApi.InitialObservers.Add(new AuditorLight());

            using (var api = SQLitePortability.CreateApi())
            {
                Assert.True(api.HasObservers());
                BeanApi.InitialObservers = new List<BeanObserver>();
            }
        }

        [Fact]
        public void Regression_NullingExistingProp()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                api.EnterFluidMode();

                var bean = api.Dispense("kind1");
                bean["p"] = 123;

                var id = api.Store(bean);

                bean["p"] = null;
                api.Store(bean);

                bean = api.Load("kind1", id);
                Assert.Null(bean["p"]);
            }
        }

        [Fact]
        public void ApiLink()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                api.EnterFluidMode();

                var bean = api.Dispense<ApiLinkChecker>();

                // Insert
                var id = api.Store(bean);
                Assert.Same(api, bean.Trace["bs"]);
                Assert.Same(api, bean.Trace["bi"]);
                Assert.DoesNotContain(bean.Trace, t => t.Key == "bu");
                Assert.Same(api, bean.Trace["ai"]);
                Assert.DoesNotContain(bean.Trace, t => t.Key == "au");
                Assert.Same(api, bean.Trace["as"]);

                // Load
                bean = api.Load<ApiLinkChecker>(id);
                Assert.Same(api, bean.Trace["bl"]);
                Assert.Same(api, bean.Trace["al"]);

                // Update
                bean["a"] = 1;
                api.Store(bean);
                Assert.Same(api, bean.Trace["bs"]);
                Assert.DoesNotContain(bean.Trace, t => t.Key == "bi");
                Assert.Same(api, bean.Trace["bu"]);
                Assert.DoesNotContain(bean.Trace, t => t.Key == "ai");
                Assert.Same(api, bean.Trace["au"]);
                Assert.Same(api, bean.Trace["as"]);

                // Trash
                api.Trash(bean);
                Assert.Same(api, bean.Trace["bt"]);
                Assert.Same(api, bean.Trace["at"]);
            }
        }

        [Fact]
        public void GetsCompoundKeyname()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                api.Key("order_item", "order_id", "product_id");
                Assert.Equal("order_id;product_id", api.GetCompoundKeyNames("order_item"));

                api.Key("order_item", "order_id");
                Assert.Equal(string.Empty, api.GetCompoundKeyNames("order_item"));
            }
        }

        [Fact]
        public void GetsNotCompoundKeyname()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                api.Key("order", "orderId");
                var bean = api.Dispense("order").Put("orderId", 1);
                Assert.Equal(1, api.GetNcKeyValue(bean));

                api.Key("order", "");
                Assert.Null(api.GetNcKeyValue(bean));

                api.Key("order", "keyField1", "keyField2");
                Assert.Throws<NotSupportedException>(() => api.GetNcKeyValue(bean));
            }
        }

        [Fact]
        public void GetsRankOfKindColumn()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                api.EnterFluidMode();

                var key = api.Dispense("foo")
                    .Put("string", "Hello!")
                    .Store();

                Assert.Equal(0, api.GetRankOfKindColumn("foo", "string"));
            }
        }

        [Fact]
        public void ExecuteInExplicitTransaction()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                // Action
                api.Transaction(() =>
                {
                    Debug.WriteLine("In Transaction");
                });

                // Func<bool>
                api.Transaction(() => true);
            }
        }

        [Fact]
        public void AuditBeanInsert()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                api.AddObserver(new Auditor(api, string.Empty));
                api.EnterFluidMode();
                
                api.Hive["CurrentUser"] = "John Doe";

                var key = api.Dispense("foo")
                    .Put("null", null)
                    .Put("bool", true)
                    .Put("sbyte", sbyte.Parse("123"))
                    .Put("ssbyte", sbyte.Parse("-123"))
                    .Put("byte", byte.Parse("123"))
                    .Put("int", 123)
                    .Put("long", 123456789L)
                    .Put("double", 123.4567)
                    .Put("decimal", 123.45m)
                    .Put("string", "Hello!")
                    .Put("datetime", new DateTime(2000, 1, 1))
                    .Put("guid", Guid.Parse("6161ADAD-72F0-48D1-ACE2-CD98315C9D5B"))
                    .Put("byte[]", Encoding.UTF8.GetBytes("Hello!"))
                    .Store();

                var audits = api.Find(false, "AUDIT", "WHERE ObjectId = {0}", key);

                Assert.Equal(12, audits.Length);
            }
        }

        [Fact]
        public void AuditLightBeanInsert()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                api.AddObserver(new AuditorLight());
                api.Hive["CurrentUser"] = "John Doe";

                api.EnterFluidMode();
                api.Dispense("foo")
                    .Put("CreatedBy", new string('X',64))
                    .Put("CreatedAt", new DateTime(2000, 1, 1))
                    .Put("ChangedBy", new string('X', 64))
                    .Put("ChangedAt", new DateTime(2000, 1, 1))
                    .Put("c1", "Hello!")
                    .Put("c2", 12345)
                    .Store();
                api.ExitFluidMode();

                var key = api.Dispense("foo")
                    .Put("CreatedBy", "...")
                    .Put("CreatedAt", new DateTime(2000, 1, 1))
                    .Put("ChangedBy", "...")
                    .Put("ChangedAt", new DateTime(2000, 1, 1))
                    .Put("c1", "Hello!")
                    .Put("c2", 12345)
                    .Store();

                var foo = api.Load("foo", key);

                Assert.Equal(api.Hive["CurrentUser"], foo["CreatedBy"]);
                Assert.Equal(api.Hive["CurrentUser"], foo["ChangedBy"]);
                Assert.Equal(DateTime.Now.ToString("yyyy-MM-dd"), foo.Get<DateTime>("CreatedAt").ToString("yyyy-MM-dd"));
                Assert.Equal(DateTime.Now.ToString("yyyy-MM-dd"), foo.Get<DateTime>("ChangedAt").ToString("yyyy-MM-dd"));
            }
        }

        [Fact]
        public void AuditBeanUpdate()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                api.AddObserver(new Auditor(api, string.Empty));
                api.EnterFluidMode();

                api.Hive["CurrentUser"] = "John Doe";

                var key = api.Dispense("foo")
                    .Put("null", null)
                    .Put("bool", true)
                    .Put("sbyte", sbyte.Parse("123"))
                    .Put("ssbyte", sbyte.Parse("-123"))
                    .Put("byte", byte.Parse("123"))
                    .Put("int", 123)
                    .Put("long", 123456789L)
                    .Put("double", 123.4567)
                    .Put("decimal", 123.45m)
                    .Put("string", "Hello!")
                    .Put("datetime", new DateTime(2000, 1, 1))
                    .Put("guid", Guid.Parse("6161ADAD-72F0-48D1-ACE2-CD98315C9D5B"))
                    .Put("byte[]", Encoding.UTF8.GetBytes("Hello!"))
                    .Store();

                var foo = api.Load("foo", key);

                foo
                    .Put("bool", false)
                    .Put("int", 4711)
                    .Put("string", "Hello World!")
                    .Store();

                var audits = api.Find(false, "Audit", "WHERE ObjectId = {0} AND Action = 'UPDATE'", key);

                Assert.Equal(3, audits.Length);
            }
        }

        [Fact]
        public void AuditLightBeanUpdate()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                api.AddObserver(new AuditorLight());
                api.Hive["CurrentUser"] = "Jane Doe";

                api.EnterFluidMode();
                var key = api.Dispense("foo")
                    .Put("CreatedBy", new string('X', 64))
                    .Put("CreatedAt", new DateTime(2000, 1, 1))
                    .Put("ChangedBy", new string('X', 64))
                    .Put("ChangedAt", new DateTime(2000, 1, 1))
                    .Put("c1", "Hello!")
                    .Put("c2", 12345)
                    .Store();
                api.ExitFluidMode();

                var foo = api.Load("foo", key);
                foo.Put("c1", "Hello World!").Store();
                foo = api.Load("foo", key);

                Assert.Equal(new string('X', 64), foo["CreatedBy"]);
                Assert.Equal(api.Hive["CurrentUser"], foo["ChangedBy"]);
                Assert.Equal(new DateTime(2000, 1, 1), foo.Get<DateTime>("CreatedAt"));
                Assert.Equal(DateTime.Now.ToString("yyyy-MM-dd"), foo.Get<DateTime>("ChangedAt").ToString("yyyy-MM-dd"));
            }
        }

        [Fact]
        public void AuditBeanTrash()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                api.AddObserver(new Auditor(api, string.Empty));
                api.EnterFluidMode();

                api.Hive["CurrentUser"] = "John Doe";

                var key = api.Dispense("foo")
                    .Put("bool", true)
                    .Store();

                var foo = api.Load("foo", key);

                foo.Trash();

                var audits = api.Find(false, "Audit", "WHERE ObjectId = {0} AND Action = 'DELETE'", key);

                Assert.NotNull(audits.FirstOrDefault()["Notes"]);
                Assert.Equal(1, audits.Length);
            }
        }


        class ThrowingBean : Bean
        {
            public bool Throw;

            public ThrowingBean()
                : base("test")
            {
            }

            protected internal override void AfterStore()
            {
                if (Throw)
                    throw new Exception();
            }

            protected internal override void AfterTrash()
            {
                if (Throw)
                    throw new Exception();
            }
        }

        class ApiLinkChecker : Bean
        {
            public Dictionary<string, BeanApi> Trace = new Dictionary<string, BeanApi>();

            public ApiLinkChecker()
                : base("foo")
            {
            }

            protected internal override void BeforeLoad()
            {
                Trace["bl"] = GetApi();
            }

            protected internal override void AfterLoad()
            {
                Trace["al"] = GetApi();
            }

            protected internal override void BeforeStore()
            {
                Trace["bs"] = GetApi();
            }

            protected internal override void BeforeInsert()
            {
                Trace["bi"] = GetApi();
            }

            protected internal override void BeforeUpdate()
            {
                Trace["bu"] = GetApi();
            }

            protected internal override void AfterStore()
            {
                Trace["as"] = GetApi();
            }

            protected internal override void AfterInsert()
            {
                Trace["ai"] = GetApi();
            }

            protected internal override void AfterUpdate()
            {
                Trace["au"] = GetApi();
            }

            protected internal override void BeforeTrash()
            {
                Trace["bt"] = GetApi();
            }

            protected internal override void AfterTrash()
            {
                Trace["at"] = GetApi();
            }
        }
    }

}
