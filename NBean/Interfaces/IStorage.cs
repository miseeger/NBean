using System.Collections.Generic;

namespace NBean.Interfaces
{
    interface IStorage
    {
        bool IsNew(string kind, IDictionary<string, object> data);
        bool IsNew(Bean bean);
        object Store(string kind, IDictionary<string, object> data, ICollection<string> dirtyNames);
        IDictionary<string, object> Load(string kind, object key);
        void Trash(string kind, object key);
    }
}
