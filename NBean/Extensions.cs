using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Mapster;
using NBean.Interfaces;
using NBean.Exceptions;
using NBean.Models;
using Sequel;

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


        public static bool IsNumeric(this string value)
        {
            var type = GetTypeAndValue(value).Item1;
            return (type != "string" && type != "DateTime");
        }


        public static bool IsDateTime(this string value)
        {
            return GetTypeAndValue(value).Item2 is DateTime;
        }


        public static object ConvertToParam(this string value)
        {
            return GetTypeAndValue(value).Item2;
        }


        public static Tuple<string, object> GetTypeAndValue(this string value)
        {
            value = value.Trim();

            if (int.TryParse(value, out var intRes))
                return Tuple.Create("int", (object)intRes);

            if (uint.TryParse(value, out var uintRes))
                return Tuple.Create("uint", (object)uintRes);

            if (long.TryParse(value, out var longRes))
                return Tuple.Create("long", (object)longRes);

            if (ulong.TryParse(value, out var ulongRes))
                return Tuple.Create("ulong", (object)ulongRes);

            // max. 5 digits precision
            if (float.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out var floatRes))
                if (floatRes.ToString(CultureInfo.CurrentCulture).Length == value.Length)
                    return Tuple.Create("float", (object)floatRes);

            // max. 13 digits precision
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out var doubleRes))
                if (doubleRes.ToString(CultureInfo.CurrentCulture).Length == value.Length)
                    return Tuple.Create("double", (object)doubleRes);

            // Any other numeric value rounded to 27 digits precision
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out var decimalRes))
                if (!value.Contains(CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator))
                    return Tuple.Create("decimal", (object)decimalRes);

            if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dateTimeRes))
                return Tuple.Create("DateTime", (object)dateTimeRes);

            return Tuple.Create("string", (object)value);
        }


        public static Tuple<string, object> GetTypeAndValueEx(this string value)
        {
            value = value.Trim();

            if (byte.TryParse(value, out var byteRes))
                return Tuple.Create("byte", (object)byteRes);

            if (sbyte.TryParse(value, out var sbyteRes))
                return Tuple.Create("sbyte", (object)sbyteRes);

            if (ushort.TryParse(value, out var ushortRes))
                return Tuple.Create("ushort", (object)ushortRes);

            if (short.TryParse(value, out var shortRes))
                return Tuple.Create("short", (object)shortRes);

            return value.GetTypeAndValue();
        }


        public static bool IsQuotedString(this string value)
        {
            return !IsNumeric(value) && value.StartsWith("'") && value.EndsWith("'");
        }


        public static bool IsQuotedDateTime(this string value)
        {
            return value.StartsWith("'") && value.EndsWith("'") && IsDateTime(value.Replace("'", ""));
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


        public static string ToJson(this object obj, bool toPrettyJson = false)
        {
            var jso = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = toPrettyJson
            };

            return JsonSerializer.Serialize(obj, jso);
        }


        public static IEnumerable<T> ToPoco<T>(this IEnumerable<Bean> beans)
        {
            return beans.ToList().Select(b => b.Export().Adapt<T>());
        }


        public static IEnumerable<T> ToPocoList<T>(this IEnumerable<IDictionary<string, object>> data)
        {
            return data.ToList().Select(b => b.Adapt<T>()).ToList();
        }


        public static Bean ToBean(this object poco, string kind)
        {
            if (poco.GetType().GetInterface("IEnumerable")  != null)
            {
                throw CannotMapIEnumerableException.Create();
            }

            var factory = new BeanFactory();

            return factory.Dispense(kind).FromPoco(poco);
        }


        public static IEnumerable<Bean> ToBeanList(this IEnumerable<object> pocos, string kind)
        {
            var factory = new BeanFactory();

            return pocos.Select(poco => factory.Dispense(kind).FromPoco(poco)).ToList();
        }


        public static IDictionary<string, object>[] Fetch(this SqlBuilder sqlBuilder, BeanApi api, 
            bool useCache = true, params object[] parameters)
        {
            var query = sqlBuilder.ToSql();

            return query.StartsWith("SELECT")
                ? api.Rows(useCache, query, parameters)
                : throw NotAnSqlQueryException.Create();
        }


        private static Pagination PrepareFetchedPagination(BeanApi api, string query, int pageNo, int perPage)
        {
            var count = api.Cell<long>(Regex.Replace(query, 
                "SELECT((.|\\n|\\r)*?)FROM", "SELECT COUNT(*) FROM"));

            return new Pagination(count, pageNo, perPage);
        }


        public static IDictionary<string, object>[] FetchPaginated(this SqlBuilder sqlBuilder, BeanApi api,
            int pageNo, int perPage = 10, bool useCache = true, params object[] parameters)
        {
            var query = sqlBuilder.ToSql();

            if (!query.StartsWith("SELECT")) 
                throw NotAnSqlQueryException.Create();

            var pagination = PrepareFetchedPagination(api, query, pageNo, perPage);
                
            var dbDetails = api.CreateDetails();

            return api.Rows(useCache, 
                $"{query} {dbDetails.Paginate(pagination.CurrentPage, perPage)}", parameters);
        }


        public static Pagination FetchLPaginated(this SqlBuilder sqlBuilder, BeanApi api,
            int pageNo, int perPage = 10, bool useCache = true, params object[] parameters)
        {
            var query = sqlBuilder.ToSql();

            if (!query.StartsWith("SELECT"))
                throw NotAnSqlQueryException.Create();

            var pagination = PrepareFetchedPagination(api, query, pageNo, perPage);

            var dbDetails = api.CreateDetails();

            pagination.Data = api.Rows(useCache,
                $"{query} {dbDetails.Paginate(pagination.CurrentPage, perPage)}", parameters);

            return pagination;
        }


        public static T[] FetchCol<T>(this SqlBuilder sqlBuilder, BeanApi api,
            bool useCache = true, params object[] parameters)
        {
            var query = sqlBuilder.ToSql();

            return query.StartsWith("SELECT")
                ? api.Col<T>(useCache, query, parameters)
                : throw NotAnSqlQueryException.Create();
        }


        public static T FetchScalar<T>(this SqlBuilder sqlBuilder, BeanApi api,
            bool useCache = true, params object[] parameters)
        {
            var query = sqlBuilder.ToSql();

            return query.StartsWith("SELECT")
                ? api.Cell<T>(useCache, query, parameters)
                : throw NotAnSqlQueryException.Create();
        }


        public static IEnumerable<IDictionary<string, object>> ToRowsIterator(this SqlBuilder sqlBuilder, BeanApi api,
            params object[] parameters)
        {
            var query = sqlBuilder.ToSql();

            return query.StartsWith("SELECT")
                ? api.RowsIterator(query, parameters)
                : throw NotAnSqlQueryException.Create();
        }


        public static IEnumerable<T> ToColIterator<T>(this SqlBuilder sqlBuilder, BeanApi api,
            params object[] parameters)
        {
            var query = sqlBuilder.ToSql();

            return query.StartsWith("SELECT")
                ? api.ColIterator<T>(query, parameters)
                : throw NotAnSqlQueryException.Create();
        }


        public static int Execute(this SqlBuilder sqlBuilder, BeanApi api, params object[] parameters)
        {
            var query = sqlBuilder.ToSql();

            return query.StartsWith("SELECT")
                ? throw NotExecutableException.Create()
                : api.Exec(query, parameters);
        }

    }

}
