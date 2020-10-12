using System.Collections.Generic;

namespace NBean.Interfaces
{
    public interface IHive
    {
        object this[string key] { get; set; }

        void Clear(string key);
        void ClearAll();
        void Delete(string key);
        void DeleteAll();
        bool Exists(string key);
	}
}