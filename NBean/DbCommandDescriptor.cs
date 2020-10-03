using System;
using System.Collections.Generic;
using System.Linq;

namespace NBean
{
    internal readonly struct DbCommandDescriptor : IEquatable<DbCommandDescriptor>
    {
        private readonly int _tag;
        public readonly string Sql;
        public readonly object[] Parameters;


        public DbCommandDescriptor(string sql, params object[] parameters)
            : this(0, sql, parameters)
        {
        }


        public DbCommandDescriptor(int tag, string sql, params object[] parameters)
        {
            _tag = tag;
            Sql = sql;
            Parameters = parameters ?? new object[] { null };
        }


#if DEBUG
        public override string ToString()
        {
            var text = "[" + _tag + "] " + Sql;

            if (Parameters.Any())
                text += " WITH " + string.Join(", ", Parameters);

            return text;
        }
#endif


        public bool Equals(DbCommandDescriptor other)
        {
            return _tag == other._tag
                && Sql == other.Sql
                && ArraysEqual(Parameters, other.Parameters);
        }


        public override bool Equals(object obj)
        {
            return obj is DbCommandDescriptor descriptor && Equals(descriptor);
        }


        public override int GetHashCode()
        {
            var hash = CombineHashCodes(_tag, Sql.GetHashCode());

            return Parameters
                .Aggregate(hash, (current, value) => CombineHashCodes(current,
                    EqualityComparer<object>.Default.GetHashCode(value)));
        }

        private static int CombineHashCodes(int h1, int h2)
        {
            // from System.Web.Util.HashCodeCombiner
            return (h1 << 5) + h1 ^ h2;
        }


        private static bool ArraysEqual<T>(T[] x, T[] y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (x is null || y is null)
                return false;

            return x.SequenceEqual(y);
        }

    }

}
