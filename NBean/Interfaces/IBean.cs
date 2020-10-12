using System.Collections.Generic;

namespace NBean.Interfaces 
{
    public interface IBean {
        string GetKind();

        bool AuditChanges { get; set; }
        object this[string name] { get; set; }

        T Get<T>(string name);
        Bean Put(string name, object value);
        IEnumerable<string> Columns { get; }
    }
}
