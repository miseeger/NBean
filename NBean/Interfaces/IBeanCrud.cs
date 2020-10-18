using System.Collections.Generic;

namespace NBean.Interfaces 
{
    public interface IBeanCrud : IBeanDispenser, IObserverSupport
    {
        bool DirtyTracking { get; set; }

        Bean RowToBean(string kind, IDictionary<string, object> row);
        T RowToBean<T>(IDictionary<string, object> row) where T : Bean, new();

        Bean Load(string kind, object key);
        T Load<T>(object key) where T : Bean, new();

        object Store(Bean bean);

        void Trash(Bean bean);
    }
}
