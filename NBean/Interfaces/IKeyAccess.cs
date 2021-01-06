using System.Collections.Generic;

namespace NBean.Interfaces
{
    interface IKeyAccess
    {
        bool IsAutoIncrement(string kind);
        bool IsAutoIncrementReplaced();
        IReadOnlyList<string> GetKeyNames(string kind);
        object GetKey(string kind, IDictionary<string, object> data);
        void SetKey(string kind, IDictionary<string, object> data, object key);
    }
}
