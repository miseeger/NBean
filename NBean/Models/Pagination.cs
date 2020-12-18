using System.Collections;
using System.Collections.Generic;

namespace NBean.Models
{
    public class Pagination
    {
        public IDictionary<string, object>[] Data { get; set; }
        public long Total { get; set; }
        public int PerPage { get; set; }
        public int CurrentPage { get; set; }
        public int LastPage { get; set; }
        public int NextPage { get; set; }
        public int PrevPage { get; set; }
        public long From { get; set; }
        public long To { get; set; }
    }
}