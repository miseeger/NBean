using System;
using System.Reflection;

namespace NBean  {

#if NETSTANDARD

    partial class Extensions {
        internal static bool IsEnum(this Type type) {
            return type.GetTypeInfo().IsEnum;
        }
        internal static bool IsGenericType(this Type type) {
            return type.GetTypeInfo().IsGenericType;
        }
    }

#else

    [Serializable]
    partial class Bean {
    }

    partial class Extensions {
        internal static bool IsEnum(this Type type) {
            return type.IsEnum;
        }
        internal static bool IsGenericType(this Type type) {
            return type.IsGenericType;
        }
    }

#endif

#if !NETSTANDARD

    partial class BeanApi {
        public BeanApi(string connectionString, string providerName)
            : this(connectionString, DbProviderFactories.GetFactory(providerName)) {
        }
    }

#endif

}


