using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NBean.Tests {
    
    public class BeanTests {

        [Fact]
        public void Indexer() {
            var bean = new Bean();

            Assert.Null(bean["x"]);

            bean["x"] = "123";
            Assert.Equal("123", bean["x"]);

            bean["x"] = null;
            Assert.Null(bean["x"]);

            bean["x"] = new int?(123);
            Assert.Equal(123, bean["x"]);
        }

        [Fact]
        public void TypedAccessors() {
            var bean = new Bean();

            Assert.Equal(0, bean.Get<int>("x"));
            Assert.Null(bean.Get<int?>("x"));

            bean.Put("x", 0);
            Assert.Equal(0, bean.Get<int>("x"));
            Assert.Equal(0, bean.Get<int?>("x"));

            bean.Put("x", null);
            Assert.Equal(0, bean.Get<int>("x"));
            Assert.Null(bean.Get<int?>("x"));

            bean.Put("x", new int?(1));
            Assert.Equal(1, bean.Get<int>("x"));
        }

        [Fact]
        public void TypedAccessors_Conversion() {
            AssertExtensions.WithCulture("de-DE", () => 
            {
                var bean = new Bean();

                bean.Put("x", "3.14");
                Assert.Equal(3.14, bean.Get<double?>("x"));
                Assert.Equal(3.14M, bean.Get<decimal>("x"));

                bean.Put("x", "abc");
                Assert.Equal(0, bean.Get<int>("x"));
                Assert.Null(bean.Get<int?>("x"));
            });
        }

        [Fact]
        public void TypedAccessors_Enums() {
            var bean = new Bean();

            bean.Put("x", DayOfWeek.Thursday);
            Assert.Equal(DayOfWeek.Thursday, bean.Get<DayOfWeek>("x"));

            bean.Put("x", "THURSDAY");
            Assert.Equal(DayOfWeek.Thursday, bean.Get<DayOfWeek>("x"));

            bean.Put("x", (ulong)DayOfWeek.Thursday);
            Assert.Equal(DayOfWeek.Thursday, bean.Get<DayOfWeek>("x"));

            bean.Put("x", "?");
            Assert.Equal(default(DayOfWeek), bean.Get<DayOfWeek>("x"));

            bean.Put("x", 4);
            Assert.Equal(DayOfWeek.Thursday, bean.Get<DayOfWeek?>("x"));
        }

        [Fact]
        public void TypedAccessors_Dates() {
            var bean = new Bean();

            bean.Put("x", new DateTime(2011, 11, 11));
            Assert.Equal(2011, bean.Get<DateTime>("x").Year);

            bean.Put("x", "2012-12-12");
            Assert.Equal(2012, bean.Get<DateTime>("x").Year);

            bean.Put("x", "?");
            Assert.Equal(new DateTime(), bean.Get<DateTime>("x"));
        }

        [Fact]
        public void TypedAccessors_NonConvertile() {
            var guid = Guid.NewGuid();
            var bean = new Bean();
            bean.Put("p", guid);
            Assert.Equal(guid, bean.Get<Guid>("p"));
            Assert.Equal(guid, bean.Get<Guid?>("p"));
        }

        [Fact]
        public void Kind() { 
            var bean = new Bean("kind1");
            Assert.Equal("kind1", bean.GetKind());
        }

        [Fact]
        public void ToStringMethod() {
            Assert.Equal("NBean.Bean", new Bean().ToString());
            Assert.Equal("product", new Bean("product").ToString());
        }

        [Fact]
        public void Columns() {
            var bean = new Bean
            {
                ["a"] = 1, 
                ["b"] = 2, 
                ["c"] = null
            };

            var expect = new List<string>(new[] { "a", "b", "c" });
            
            Assert.Equal(3, bean.Columns.Count());
            foreach (var s in bean.Columns) {
                Assert.Contains(s, expect);
                expect.Remove(s);
            }
            Assert.Empty(expect);
        }

        [Fact]
        public void Export() {
            var bean = new Bean
            {
                ["id"] = 123
            };

            bean.Put("a", 1).Put("b", "abc");

            AssertExtensions.Equivalent(bean.Export(), new Dictionary<string, object> { 
                { "id", 123 }, { "a", 1 }, { "b", "abc" }
            });

            Assert.NotSame(bean.Export(), bean.Export());

            AssertExtensions.Equivalent(bean.Export("id"), new Dictionary<string, object> {
                { "a", 1 }, { "b", "abc" }
            });

            Assert.NotSame(bean.Export("a"), bean.Export("a"));
        }

        [Fact]
        public void ExportWithIgnorelist()
        {
            var bean = new Bean
            {
                ["id"] = 123
            };

            bean.Put("a", 1).Put("b", "abc");

            AssertExtensions.Equivalent(bean.Export("id"), new Dictionary<string, object> {
                { "a", 1 }, { "b", "abc" }
            });
            AssertExtensions.Equivalent(bean.Export("id,b"), new Dictionary<string, object> {
                { "a", 1 }
            });
            AssertExtensions.Equivalent(bean.Export("id,a,b"), new Dictionary<string, object>());

            Assert.NotSame(bean.Export("id"), bean.Export("id"));
            Assert.NotSame(bean.Export("id,b"), bean.Export("id,b"));
            Assert.NotSame(bean.Export("id,a,b"), bean.Export("id,a,b"));
        }

        [Fact]
        public void Import() {
            var bean = new Bean
            {
                ["a"] = 1, 
                ["b"] = 1, 
                ["c"] = 1
            };

            var data = new Dictionary<string, object> { { "b", 2 }, { "c", null } };
            bean.Import(data);

            Assert.Equal(1, bean["a"]);
            Assert.Equal(2, bean["b"]);
            Assert.Null(bean["c"]);

            data["b"] = "changed";
            Assert.Equal(2, bean["b"]);
        }

        [Fact]
        public void GetDrityNames() {
            var bean = new Bean();
            Assert.Empty(bean.GetDirtyNames());

            bean["a"] = 1;
            AssertExtensions.Equivalent(new[] { "a" }, bean.GetDirtyNames());

            bean["a"] = null;
            Assert.Empty(bean.GetDirtyNames());

            bean["a"] = 1;
            bean.ForgetDirtyBackup();
            Assert.Empty(bean.GetDirtyNames());

            bean["a"] = null;
            AssertExtensions.Equivalent(new[] { "a" }, bean.GetDirtyNames());
        }
    }


}
