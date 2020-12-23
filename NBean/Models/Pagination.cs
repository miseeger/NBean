using System.Collections.Generic;

namespace NBean.Models
{
    public class PaginationBase
    {
        public long Total { get; set; }
        public int PerPage { get; set; }
        public int CurrentPage { get; set; }
        public int LastPage { get; set; }
        public int NextPage { get; set; }
        public int PrevPage { get; set; }
        public long From { get; set; }
        public long To { get; set; }


        protected PaginationBase(long totalRows, int pageNo, int perPage = 10)
        {
            pageNo = pageNo < 1 ? 1 : pageNo;

            var fullPages = (int)(totalRows / perPage);
            var maxPages = (fullPages * perPage) < totalRows ? fullPages + 1 : fullPages;

            Total = totalRows;
            PerPage = perPage;
            CurrentPage = pageNo > maxPages ? maxPages : pageNo;
            LastPage = maxPages;
            NextPage = CurrentPage == LastPage ? -1 : CurrentPage + 1;
            PrevPage = CurrentPage == 1 ? -1 : CurrentPage - 1;
            From = ((CurrentPage - 1) * PerPage) + 1;
            To = CurrentPage * PerPage > Total ? Total : CurrentPage * PerPage;
        }
    }

    
    public class Pagination : PaginationBase
    {
        public IDictionary<string, object>[] Data { get; set; }
        
        public Pagination(long totalRows, int pageNo, int perPage = 10)
            :base(totalRows, pageNo, perPage)
        {
        }
    }


    public class Pagination<T> : PaginationBase where T : Bean, new()
    {
        public T[] Data { get; set; }

        public Pagination(long totalRows, int pageNo, int perPage = 10)
            : base(totalRows, pageNo, perPage)
        {
        }
    }

}