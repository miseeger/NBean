using System.Linq;
using NBean.Exceptions;
using Xunit;
using Sequel;

namespace NBean.Tests
{

    public class SequelQueryBuilderTests
    {
        [Fact]
        public void SelectsRows()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                api.Exec("CREATE TABLE Product (id INTEGER NOT NULL PRIMARY KEY, Name)");
                api.Dispense("Product").Put("Name", "MacBook Pro 13").Store();
                api.Dispense("Product").Put("Name", "Microsoft Surface IV").Store();
                api.Dispense("Product").Put("Name", "Lenovo ThinkPad X1").Store();
                api.Dispense("Product").Put("Name", "Dell XPS 13").Store();
                api.Dispense("Product").Put("Name", "Lenovo Yoga").Store();

                var result = new SqlBuilder()
                    .Select("*")
                    .From("Product")
                    .Fetch(api);

                Assert.True(result.Length == 5);
            }
        }


        [Fact]
        public void IteratesRows()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                api.Exec("CREATE TABLE Product (id INTEGER NOT NULL PRIMARY KEY, Name)");
                api.Dispense("Product").Put("Name", "MacBook Pro 13").Store();
                api.Dispense("Product").Put("Name", "Microsoft Surface IV").Store();
                api.Dispense("Product").Put("Name", "Lenovo ThinkPad X1").Store();
                api.Dispense("Product").Put("Name", "Dell XPS 13").Store();
                api.Dispense("Product").Put("Name", "Lenovo Yoga").Store();

                var iterator = new SqlBuilder()
                    .Select("*")
                    .From("Product")
                    .ToIterator(api);

                Assert.True(iterator.Count() == 5);
            }
        }


        [Fact]
        public void ThrowsNotAnSqlQueryException()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                api.Exec("CREATE TABLE Product (id INTEGER NOT NULL PRIMARY KEY, Name)");

                Assert.Throws<NotAnSqlQueryException>(() => new SqlBuilder()
                    .Insert("Product")
                    .Into("Id", "Name")
                    .Values("1", "'MacBook Pro 13'")
                    .Fetch(api));
            }
        }


        [Fact]
        public void InsertsRow()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                api.Exec("CREATE TABLE Product (Id INTEGER NOT NULL PRIMARY KEY, Name)");

                var result = new SqlBuilder()
                    .Insert("Product")
                    .Into("Id","Name")
                    .Values("1", "'MacBook Pro 13'")
                    .Execute(api);

                Assert.True(result == 1 && api.Count("Product") == 1);
            }
        }


        [Fact]
        public void ThrowsNotExecutableException()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                api.Exec("CREATE TABLE Product (id INTEGER NOT NULL PRIMARY KEY, Name)");

                Assert.Throws<NotExecutableException>(() => new SqlBuilder()
                    .Select("*")
                    .From("Product")
                    .Execute(api));
            }
        }

    }

}
