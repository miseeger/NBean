using System;

namespace NBean.Interfaces
{

    public interface IPluginSupport
    {
        void RegisterAction(string name, Action<BeanApi, object[]> action);
        void RegisterBeanAction(string name, Action<Bean, object[]> action);
        void RegisterFunc(string name, Func<BeanApi, object[], object> function);
        void RegisterBeanFunc(string name, Func<Bean, object[], object> function);

        object Invoke(string name, params object[] args);
    }


}
