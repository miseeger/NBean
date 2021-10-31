using System.Collections.Generic;
using NBean.Exceptions;
using NBean.Models;
using NBean.SqlBuilder.Exceptions;

namespace NBean.SqlBuilder
{

    public static class Extensions
    {

        public static IDictionary<string, object>[] Fetch(this Sequel.SqlBuilder sqlBuilder, BeanApi api,
            bool useCache = true, params object[] parameters)
        {
            var query = sqlBuilder.ToSql();

            return query.StartsWith("SELECT")
                ? api.Rows(useCache, query, parameters)
                : throw NotAnSqlQueryException.Create();
        }


        public static IDictionary<string, object>[] FetchPaginated(this Sequel.SqlBuilder sqlBuilder,
            BeanApi api, int pageNo, int perPage = 10, bool useCache = true, params object[] parameters)
        {
            var query = sqlBuilder.ToSql();

            if (!query.StartsWith("SELECT"))
                throw NotAnSqlQueryException.Create();

            var pagination = NBean.Extensions.PrepareFetchedPagination(api, query, pageNo, perPage);

            var dbDetails = api.CreateDetails();

            return api.Rows(useCache,
                $"{query} {dbDetails.Paginate(pagination.CurrentPage, perPage)}", parameters);
        }


        public static Pagination FetchLPaginated(this Sequel.SqlBuilder sqlBuilder, BeanApi api,
            int pageNo, int perPage = 10, bool useCache = true, params object[] parameters)
        {
            var query = sqlBuilder.ToSql();

            if (!query.StartsWith("SELECT"))
                throw NotAnSqlQueryException.Create();

            var pagination = NBean.Extensions.PrepareFetchedPagination(api, query, pageNo, perPage);

            var dbDetails = api.CreateDetails();

            pagination.Data = api.Rows(useCache,
                $"{query} {dbDetails.Paginate(pagination.CurrentPage, perPage)}", parameters);

            return pagination;
        }


        public static T[] FetchCol<T>(this Sequel.SqlBuilder sqlBuilder, BeanApi api,
            bool useCache = true, params object[] parameters)
        {
            var query = sqlBuilder.ToSql();

            return query.StartsWith("SELECT")
                ? api.Col<T>(useCache, query, parameters)
                : throw NotAnSqlQueryException.Create();
        }


        public static T FetchScalar<T>(this Sequel.SqlBuilder sqlBuilder, BeanApi api,
            bool useCache = true, params object[] parameters)
        {
            var query = sqlBuilder.ToSql();

            return query.StartsWith("SELECT")
                ? api.Cell<T>(useCache, query, parameters)
                : throw NotAnSqlQueryException.Create();
        }


        public static IEnumerable<IDictionary<string, object>> ToRowsIterator(this Sequel.SqlBuilder sqlBuilder, BeanApi api,
            params object[] parameters)
        {
            var query = sqlBuilder.ToSql();

            return query.StartsWith("SELECT")
                ? api.RowsIterator(query, parameters)
                : throw NotAnSqlQueryException.Create();
        }


        public static IEnumerable<T> ToColIterator<T>(this Sequel.SqlBuilder sqlBuilder, BeanApi api,
            params object[] parameters)
        {
            var query = sqlBuilder.ToSql();

            return query.StartsWith("SELECT")
                ? api.ColIterator<T>(query, parameters)
                : throw NotAnSqlQueryException.Create();
        }


        public static int Execute(this Sequel.SqlBuilder sqlBuilder, BeanApi api, params object[] parameters)
        {
            var query = sqlBuilder.ToSql();

            return query.StartsWith("SELECT")
                ? throw NotExecutableException.Create()
                : api.Exec(query, parameters);
        }

    }

}
