using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

using NBean.Interfaces;

namespace NBean.Tests {

    public class IntegrationTests {

        [Fact]
        public void ImplicitTransactionsOnStoreAndTrash() {
            using(var conn = SQLitePortability.CreateConnection()) {
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
        public void DisableImplicitTransactions() {
            using(var api = SQLitePortability.CreateApi()) {
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

            Assert.Equal("SQLite", new BeanApi(SQLitePortability.CreateConnection()).CreateDetails().DbName);

#if !NO_MSSQL
            Assert.Equal("MsSql", new BeanApi(new System.Data.SqlClient.SqlConnection()).CreateDetails().DbName);
#endif

#if !NO_MARIADB
            Assert.Equal("MariaDB", new BeanApi(new MySql.Data.MySqlClient.MySqlConnection()).CreateDetails().DbName);
#endif

#if !NO_PGSQL
            Assert.Equal("PgSql", new BeanApi(new Npgsql.NpgsqlConnection()).CreateDetails().DbName);
#endif
        }

        [Fact]
        public void Regression_NullingExistingProp() {
            using(var api = SQLitePortability.CreateApi()) {
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
        public void ApiLink() {
            using(var api = SQLitePortability.CreateApi()) {
                api.EnterFluidMode();

                var bean = api.Dispense<ApiLinkChecker>();

                // Insert
                var id = api.Store(bean);
                Assert.Same(api, bean.Trace["bs"]);
                Assert.Same(api, bean.Trace["bi"]);
                Assert.False(bean.Trace.Any(t => t.Key == "bu"));
                Assert.Same(api, bean.Trace["ai"]);
                Assert.False(bean.Trace.Any(t => t.Key == "au"));
                Assert.Same(api, bean.Trace["as"]);

                // Load
                bean = api.Load<ApiLinkChecker>(id);
                Assert.Same(api, bean.Trace["bl"]);
                Assert.Same(api, bean.Trace["al"]);

                // Update
                bean["a"] = 1;
                api.Store(bean);
                Assert.Same(api, bean.Trace["bs"]);
                Assert.False(bean.Trace.Any(t => t.Key == "bi"));
                Assert.Same(api, bean.Trace["bu"]);
                Assert.False(bean.Trace.Any(t => t.Key == "ai"));
                Assert.Same(api, bean.Trace["au"]);
                Assert.Same(api, bean.Trace["as"]);

                // Trash
                api.Trash(bean);
                Assert.Same(api, bean.Trace["bt"]);
                Assert.Same(api, bean.Trace["at"]);
            }
        }

        class ThrowingBean : Bean {
            public bool Throw;

            public ThrowingBean()
                : base("test") {
            }

            protected internal override void AfterStore() {
                if(Throw)
                    throw new Exception();
            }

            protected internal override void AfterTrash() {
                if(Throw)
                    throw new Exception();
            }
        }

        class ApiLinkChecker : Bean {
            public Dictionary<string, BeanApi> Trace = new Dictionary<string, BeanApi>();

            public ApiLinkChecker() 
                : base("foo") {
            }

            protected internal override void BeforeLoad() {
                Trace["bl"] = GetApi();
            }

            protected internal override void AfterLoad() {
                Trace["al"] = GetApi();
            }

            protected internal override void BeforeStore() {
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

            protected internal override void AfterStore() {
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

            protected internal override void BeforeTrash() {
                Trace["bt"] = GetApi();
            }

            protected internal override void AfterTrash() {
                Trace["at"] = GetApi();
            }
        }
    }

}
