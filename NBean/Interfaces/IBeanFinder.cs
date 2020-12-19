using System.Collections.Generic;
using NBean.Models;

namespace NBean.Interfaces 
{
    public interface IBeanFinder 
    {
        Bean[] Find(bool useCache, string kind, string expr = null, params object[] parameters);
        T[] Find<T>(bool useCache, string expr = null, params object[] parameters) 
            where T : Bean, new();

        Bean FindOne(bool useCache, string kind, string expr = null, params object[] parameters);
        T FindOne<T>(bool useCache, string expr = null, params object[] parameters) 
            where T : Bean, new();

        Bean[] Paginate(bool useCache, string kind, int pageNo, int perPage = 10,
            string propsIgnorelist = "", string expr = null, params object[] parameters);

        T[] Paginate<T>(bool useCache, int pageNo, int perPage = 10,
            string propsIgnorelist = "", string expr = null, params object[] parameters) 
            where T : Bean, new();

        Pagination LPaginate(bool useCache, string kind, int pageNo = 1, int perPage = 10, 
            string propsIgnorelist = "", string expr = null, params object[] parameters);

        IEnumerable<Bean> FindIterator(string kind, string expr = null, params object[] parameters);

        IEnumerable<T> FindIterator<T>(string expr = null, params object[] parameters) 
            where T : Bean, new();

        long Count(bool useCache, string kind, string expr = null, params object[] parameters);

        long Count<T>(bool useCache, string expr = null, params object[] parameters) 
            where T : Bean, new();
    }
}
