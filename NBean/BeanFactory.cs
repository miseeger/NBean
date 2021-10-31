using NBean.Interfaces;

namespace NBean
{
    public class BeanFactory : IBeanFactory
    {
        public IBeanOptions _config;

        public IBeanOptions Options => _config ?? (_config = new BeanOptions());


        public BeanFactory() { }


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
