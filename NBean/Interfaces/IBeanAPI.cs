using System;
using System.Data.Common;

namespace NBean.Interfaces
{
    public interface IBeanApi : IDisposable, IBeanCrud, IBeanFinder, IDatabaseAccess, IValueRelaxations
    {
        DbConnection Connection { get; }

        void EnterFluidMode();

        void Key(string kind, string name, bool autoIncrement);
        void Key(string kind, params string[] names);
        void Key<T>(string name, bool autoIncrement) where T : Bean, new();
        void Key<T>(params string[] names) where T : Bean, new();
        void DefaultKey(bool autoIncrement);
        void DefaultKey(string name, bool autoIncrement = true);
    }
}
