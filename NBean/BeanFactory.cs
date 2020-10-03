using NBean.Interfaces;

namespace NBean
{
    internal class BeanFactory : IBeanFactory
    {
        private IBeanOptions _config;

        public IBeanOptions Options => _config ?? (_config = new BeanOptions());


        internal BeanFactory() { }


        public Bean Dispense(string kind)
        {
            return ConfigureBean(new Bean(kind));
        }


        public T Dispense<T>() where T : Bean, new()
        {
            return ConfigureBean(new T());
        }


        private T ConfigureBean<T>(T bean) where T : Bean
        {
            bean.Dispensed = true;
            bean.ValidateGetColumns = Options.ValidateGetColumns;

            return bean;
        }
    }
}
