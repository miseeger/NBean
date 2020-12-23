using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using NBean.Interfaces;
using NBean.Models;

namespace NBean
{
    internal class DatabaseBeanFinder : IBeanFinder
    {
        private readonly IDatabaseDetails _details;
        private readonly IDatabaseAccess _db;
        private readonly IBeanCrud _crud;


        public DatabaseBeanFinder(IDatabaseDetails details, IDatabaseAccess db, IBeanCrud crud)
        {
            _details = details;
            _db = db;
            _crud = crud;
        }


        // ----- Find ---------------------------------------------------------

        public Bean[] Find(bool useCache, string kind, string expr = null, params object[] parameters)
        {
            return Rows(useCache, kind, expr, parameters)
                .Select(row => _crud.RowToBean(kind, row))
                .ToArray();
        }


        public T[] Find<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new()
        {
            return Rows(useCache, Bean.GetKind<T>(), expr, parameters)
                .Select(_crud.RowToBean<T>)
                .ToArray();
        }


        // ----- Paginate -----------------------------------------------------

        public Bean[] Paginate(bool useCache, string kind, int pageNo = 1, int perPage = 10,
            string propsIgnorelist = "", string expr = null, params object[] parameters)
        {
            var pagination = new Pagination(Count(true, kind, expr, parameters), pageNo, perPage);

            return Rows(useCache, kind,
                    $"{expr}\r\n{_details.Paginate(pagination.CurrentPage, perPage)}",
                    parameters)
                .Select(row => _crud.RowToBean(kind, row).FCleanse(propsIgnorelist))
                .ToArray();
        }


        public T[] Paginate<T>(bool useCache, int pageNo = 1, int perPage = 10,
            string propsIgnorelist = "", string expr = null, params object[] parameters) 
            where T : Bean, new()
        {
            var kind = Bean.GetKind<T>();
            var pagination = new Pagination(Count(true, kind, expr, parameters), pageNo, perPage);

            return Rows(useCache, kind,
                    $"{expr}\r\n{_details.Paginate(pagination.CurrentPage, perPage)}",
                    parameters)
                .Select(row => _crud.RowToBean<T>(row).FCleanse<T>(propsIgnorelist))
                .ToArray();
        }


        public Pagination LPaginate(bool useCache, string kind, int pageNo = 1, int perPage = 10, 
            string propsIgnorelist = "", string expr = null, params object[] parameters)
        {
            var result = new Pagination(Count(true, kind, expr, parameters), pageNo, perPage);

            result.Data = Paginate(useCache, kind, result.CurrentPage, perPage, 
                propsIgnorelist, expr, parameters)
                .Select(b => b.Export())
                .ToArray();

            return result;
        }


        public Pagination<T> LPaginate<T>(bool useCache, int pageNo = 1, int perPage = 10,
            string propsIgnorelist = "", string expr = null, params object[] parameters)
            where T : Bean, new()
        {
            var kind = Bean.GetKind<T>();
            var result = new Pagination<T>(Count(true, kind, expr, parameters), pageNo, perPage);

            result.Data = Paginate<T>(useCache, result.CurrentPage, perPage,
                    propsIgnorelist, expr, parameters);

            return result;
        }


        // ----- FindOne ------------------------------------------------------

        public Bean FindOne(bool useCache, string kind, string expr = null, params object[] parameters)
        {
            return _crud.RowToBean(kind, Row(useCache, kind, expr, parameters));
        }


        public T FindOne<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new()
        {
            return _crud.RowToBean<T>(Row(useCache, Bean.GetKind<T>(), expr, parameters));
        }


        // ----- Iterators ----------------------------------------------------

        public IEnumerable<Bean> FindIterator(string kind, string expr = null, params object[] parameters)
        {
            return RowsIterator(kind, expr, parameters)
                .Select(row => _crud.RowToBean(kind, row));
        }


        public IEnumerable<T> FindIterator<T>(string expr = null, params object[] parameters) where T : Bean, new()
        {
            return RowsIterator(Bean.GetKind<T>(), expr, parameters)
                .Select(_crud.RowToBean<T>);
        }


        // ----- Count --------------------------------------------------------

        public long Count(bool useCache, string kind, string expr = null, params object[] parameters)
        {
            return _db.Cell<long>(useCache, FormatSelectQuery(kind, expr, true), parameters);
        }


        public long Count<T>(bool useCache, string expr = null, params object[] parameters) where T : Bean, new()
        {
            return Count(useCache, Bean.GetKind<T>(), expr, parameters);
        }


        // ----- Internals ----------------------------------------------------

        private IDictionary<string, object> Row(bool useCache, string kind, string expr, object[] parameters)
        {
            return _db.Row(useCache, FormatSelectQuery(kind, expr), parameters);
        }


        private IEnumerable<IDictionary<string, object>> Rows(bool useCache, string kind, string expr, object[] parameters)
        {
            return _db.Rows(useCache, FormatSelectQuery(kind, expr), parameters);
        }


        private IEnumerable<IDictionary<string, object>> RowsIterator(string kind, string expr, object[] parameters)
        {
            return _db.RowsIterator(FormatSelectQuery(kind, expr), parameters);
        }


        private string FormatSelectQuery(string kind, string expr, bool countOnly = false)
        {
            var sql = "SELECT " + (countOnly ? "COUNT(*)" : "*") + " FROM " + _details.QuoteName(kind);

            if (!string.IsNullOrEmpty(expr))
                sql += " " + expr;

            return sql;
        }
    }
}
