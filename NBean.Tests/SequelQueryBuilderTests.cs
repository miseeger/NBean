using NBean.Exceptions;
using Sequel;
using System.Linq;
using Xunit;

namespace NBean.Tests
{

    public class SequelQueryBuilderTests
    {
        private readonly BeanApi _api;

        public SequelQueryBuilderTests()
        {
            _api = SQLitePortability.CreateApi();
            _api.Exec("CREATE TABLE Product (id INTEGER NOT NULL PRIMARY KEY, Name)");
            _api.Dispense("Product").Put("Name", "MacBook Pro 13").Store();
            _api.Dispense("Product").Put("Name", "Microsoft Surface IV").Store();
            _api.Dispense("Product").Put("Name", "Lenovo ThinkPad X1").Store();
            _api.Dispense("Product").Put("Name", "Dell XPS 13").Store();
            _api.Dispense("Product").Put("Name", "Lenovo Yoga").Store();
        }


        [Fact]
        public void SelectsRows()
        {
            var result = new SqlBuilder()
                .Select("*")
                .From("Product")
                .Fetch(_api);
            Assert.True(result.Length == 5);
        }


        [Fact]
        public void PaginatesSelectedRows()
        {
            var result = new SqlBuilder()
                .Select("*")
                .From("Product")
                .FetchPaginated(_api, 0, 3);
            Assert.Equal(3, result.Length);

            result = new SqlBuilder()
                .Select("*")
                .From("Product")
                .FetchPaginated(_api, 1, 3);
            Assert.Equal(3, result.Length);

            result = new SqlBuilder()
                .Select("*")
                .From("Product")
                .FetchPaginated(_api, 2, 3);
            Assert.Equal(2, result.Length);

            result = new SqlBuilder()
                .Select("*")
                .From("Product")
                .FetchPaginated(_api, 5, 3);
            Assert.Equal(2, result.Length);

            Assert.Throws<NotAnSqlQueryException>(() =>
            {
                result = new SqlBuilder()
                    .Insert("*")
                    .From("Product")
                    .FetchPaginated(_api, 5, 3);
            });
        }


        [Fact]
        public void LPaginatesSelectedRows()
        {
            var result = new SqlBuilder()
                .Select("*")
                .From("Product")
                .FetchLPaginated(_api, 0, 3);
            Assert.Equal(1, result.CurrentPage);
            Assert.Equal(3, result.Data.Length);

            result = new SqlBuilder()
                .Select("*")
                .From("Product")
                .FetchLPaginated(_api, 1, 3);
            Assert.Equal(1, result.CurrentPage);
            Assert.Equal(3, result.Data.Length);

            result = new SqlBuilder()
                .Select("*")
                .From("Product")
                .FetchLPaginated(_api, 2, 3);
            Assert.Equal(2, result.CurrentPage);
            Assert.Equal(2, result.Data.Length);

            result = new SqlBuilder()
                .Select("*")
                .From("Product")
                .FetchLPaginated(_api, 5, 3);
            Assert.Equal(2, result.CurrentPage);
            Assert.Equal(2, result.Data.Length);

            Assert.Throws<NotAnSqlQueryException>(() =>
            {
                result = new SqlBuilder()
                    .Insert("*")
                    .From("Product")
                    .FetchLPaginated(_api, 5, 3);
            });
        }


        [Fact]
        public void SelectsSingleColumnValues()
        {
            var result = new SqlBuilder()
                .Select("Name")
                .From("Product")
                .FetchCol<string>(_api);

            Assert.True(result.Length == 5);
        }


        [Fact]
        public void SelectsSingleValue()
        {
            var result = new SqlBuilder()
                .Select("COUNT(*) AS Cnt")
                .From("Product")
                .FetchScalar<int>(_api);

            Assert.True(result == 5);
        }


        [Fact]
        public void IteratesRows()
        {
            var iterator = new SqlBuilder()
                .Select("*")
                .From("Product")
                .ToRowsIterator(_api);

            Assert.True(iterator.Count() == 5);
        }


        [Fact]
        public void IteratesSingleColumValues()
        {
            var iterator = new SqlBuilder()
                .Select("Name")
                .From("Product")
                .ToColIterator<string>(_api);

            Assert.True(iterator.Count() == 5);
        }


        [Fact]
        public void ThrowsNotAnSqlQueryException()
        {
            Assert.Throws<NotAnSqlQueryException>(() => new SqlBuilder()
                .Insert("Product")
                .Into("Id", "Name")
                .Values("1", "'MacBook Pro 13'")
                .Fetch(_api));
        }


        [Fact]
        public void InsertsRow()
        {
            var result = new SqlBuilder()
                .Insert("Product")
                .Into("Id", "Name")
                .Values("6", "'High Power Gamer Notebook'")
                .Execute(_api);

            Assert.True(result == 1 && _api.Count("Product") == 6);
        }


        [Fact]
        public void ThrowsNotExecutableException()
        {
            Assert.Throws<NotExecutableException>(() => new SqlBuilder()
                .Select("*")
                .From("Product")
                .Execute(_api));
        }

    }

}
