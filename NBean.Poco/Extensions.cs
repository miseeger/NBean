using System.Collections.Generic;
using System.Linq;
using Mapster;
using NBean.Exceptions;

namespace NBean.Poco
{

    public static class Extensions
    {
        public static IEnumerable<T> ToPocoList<T>(this IEnumerable<Bean> beans, string propsIgnorelist = "")
        {
            return beans.ToList().Select(b => b.Export(propsIgnorelist).Adapt<T>());
        }


        /// <summary>
        /// Maps / exports a Bean's Data to a (newly created) Poco of the given
        /// Type. Poco properties and Bean properties must match exactly.
        /// </summary>
        /// <typeparam name="T">Type of Poco to map to</typeparam>
        /// <param name="propsIgnorelist">The comma separated list of
        /// props (case sensitive) to be ignored.</param>
        /// <returns></returns>
        public static T ToPoco<T>(this Bean bean, string propsIgnorelist = "")
        {
            return bean.Export(propsIgnorelist).Adapt<T>();
        }


        public static IEnumerable<T> ToPocoList<T>(this IEnumerable<IDictionary<string, object>> data)
        {
            return data.ToList().Select(b => b.Adapt<T>()).ToList();
        }


        public static T ToPoco<T>(this IDictionary<string, object> data)
        {
            return data.Adapt<T>();
        }

        /// <summary>
        /// Imports / maps the given Poco to the current Bean.
        /// Poco properties and Bean properties must match exactly.
        /// </summary>
        /// <param name="poco">Simple Poco instance</param>
        public static Bean ImportPoco(this Bean bean, object poco)
        {
            var config = new TypeAdapterConfig();
            config.ForType(poco.GetType(), typeof(Dictionary<string, object>)).IgnoreNullValues(true);

            return bean.Import(poco.Adapt<Dictionary<string, object>>(config));
        }


        public static Bean ToBean(this object poco, string kind)
        {
            if (poco.GetType().GetInterface("IEnumerable") != null)
            {
                throw CannotMapIEnumerableException.Create();
            }

            var factory = new BeanFactory();

            return factory.Dispense(kind).ImportPoco(poco);
        }


        public static IEnumerable<Bean> ToBeanList(this IEnumerable<object> pocos, string kind)
        {
            var factory = new BeanFactory();

            return pocos.Select(poco => factory.Dispense(kind).ImportPoco(poco)).ToList();
        }

    }

}
