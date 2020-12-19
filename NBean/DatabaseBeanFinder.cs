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

        private int CalcMaxPages(long totalRows, int perPage)
        {
            var fullPages = (int)(totalRows / perPage);

            return 
                (fullPages * perPage) < totalRows ? fullPages + 1 : fullPages;
        }


        public Bean[] Paginate(bool useCache, string kind, int pageNo = 1, int perPage = 10,
            string propsIgnorelist = "", string expr = null, params object[] parameters)
        {
            var maxPages = CalcMaxPages(Count(true, kind, expr, parameters), perPage);

            return Rows(useCache, kind,
                    $"{expr}\r\n{_details.Paginate(pageNo > maxPages ? maxPages : pageNo, perPage)}",
                    parameters)
                .Select(row => _crud.RowToBean(kind, row).FCleanse(propsIgnorelist))
                .ToArray();
        }


        public T[] Paginate<T>(bool useCache, int pageNo = 1, int perPage = 10,
            string propsIgnorelist = "", string expr = null, params object[] parameters) 
            where T : Bean, new()
        {
            var kind = Bean.GetKind<T>();
            var maxPages = CalcMaxPages(Count(true, kind, expr, parameters), perPage);

            return Rows(useCache, kind,
                    $"{expr}\r\n{_details.Paginate(pageNo > maxPages ? maxPages : pageNo, perPage)}",
                    parameters)
                .Select(row => _crud.RowToBean<T>(row).FCleanse<T>())
                .ToArray();
        }


        public Pagination LPaginate(bool useCache, string kind, int pageNo = 1, int perPage = 10, 
            string propsIgnorelist = "", string expr = null, params object[] parameters)
        {
            pageNo = pageNo < 1 ? 1 : pageNo;

            var totalRows = Count(true, kind, expr, parameters);
            var maxPages = CalcMaxPages(totalRows, perPage);
            var currentPage = pageNo > maxPages ? maxPages : pageNo;

            return new Pagination
            {
                Data = Paginate(useCache, kind, currentPage, perPage, propsIgnorelist, 
                    expr, parameters)
                    .Select(b => b.Export())
                    .ToArray(),
                Total = Count(true, kind, expr, parameters),
                PerPage = perPage,
                CurrentPage = currentPage,
                LastPage = maxPages,
                NextPage = currentPage == maxPages ? -1 : currentPage + 1,
                PrevPage = currentPage == 1 ? -1 : currentPage - 1,
                From = ((currentPage - 1) * perPage) + 1,
                To = currentPage * perPage > totalRows ? totalRows : currentPage * perPage
            };

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
