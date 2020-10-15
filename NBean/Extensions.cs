using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NBean.Interfaces;

namespace NBean
{

    static partial class Extensions
    {
        internal static V GetSafe<K, V>(this IDictionary<K, V> dict, K key, V defaultValue = default(V))
        {
            return dict.TryGetValue(key, out var existingValue)
                ? existingValue
                : defaultValue;
        }


        internal static string GetAutoIncrementName(this IKeyAccess keyAccess, string kind)
        {
            return !keyAccess.IsAutoIncrement(kind)
                ? null
                : keyAccess.GetKeyNames(kind).First();
        }


        internal static bool IsSignedByteRange(this long value)
        {
            return -128L <= value && value <= 127L;
        }


        internal static bool IsUnsignedByteRange(this long value)
        {
            return 0L <= value && value <= 255L;
        }


        internal static bool IsInt32Range(this long value)
        {
            return -0x80000000L <= value && value <= 0x7FFFFFFFL;
        }


        internal static bool IsInt53Range(this long value)
        {
            return -0x1fffffffffffffL <= value && value <= 0x1fffffffffffffL;
        }

        internal static bool IsSafeInteger(this double value)
        {
            const double
                min = -0x1fffffffffffff,
                max = 0x1fffffffffffff;

            return Math.Truncate(value) == value && value >= min && value <= max;
        }


        private static bool IsEnum(this Type type)
        {
            return type.GetTypeInfo().IsEnum;
        }


        private static bool IsGenericType(this Type type)
        {
            return type.GetTypeInfo().IsGenericType;
        }


        internal static T ConvertSafe<T>(this object value)
        {
            switch (value)
            {
                case null:
                    return default(T);
                case T typeValue:
                    return typeValue;
                default:
                    {
                        var targetType = typeof(T);

                        try
                        {
                            if (targetType.IsGenericType() 
                                && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                targetType = Nullable.GetUnderlyingType(targetType);

                            if (targetType == typeof(Guid))
                                return (T)Activator.CreateInstance(targetType, value);

                            if (targetType.IsEnum())
                                return (T)Enum.Parse(targetType, Convert.ToString(value), true);

                            return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
                        }
                        catch
                        {
                            return default(T);
                        }
                    }
            }
        }


        public static string FormatValueToString(this object value)
        {
            switch (value)
            {
                case null:
                    return "#NULL#";
                case bool b:
                    return b ? "true" : "false";
                case sbyte sb:
                    return sb.ToString(NumberFormatInfo.CurrentInfo);
                case byte b:
                    return b.ToString(NumberFormatInfo.CurrentInfo);
                case int i:
                    return i.ToString(NumberFormatInfo.CurrentInfo);
                case long l:
                    return l.ToString(NumberFormatInfo.CurrentInfo);
                case double d:
                    return d.ToString(NumberFormatInfo.CurrentInfo);
                case decimal de:
                    return de.ToString(NumberFormatInfo.CurrentInfo);
                case string s:
                    return s;
                case DateTime dt:
                    return dt.ToString("yyyy-MM-ddTHH:mm:ss");
                case Guid g:
                    return g.ToString();
                case byte[] ba:
                    return System.Text.Encoding.Default.GetString(ba); ;
                default:
                    return value.ToString();
            }
        }

    }
}
