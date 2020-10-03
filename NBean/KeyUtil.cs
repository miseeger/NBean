using System;
using System.Collections.Generic;
using System.Linq;
using NBean.Interfaces;

namespace NBean
{
    class KeyUtil : IKeyAccess
    {
        private readonly IDictionary<string, ICollection<string>> _names =
            new Dictionary<string, ICollection<string>>();
        private readonly IDictionary<string, bool> _autoIncrements =
            new Dictionary<string, bool>();

        public string DefaultName = "id";
        public bool DefaultAutoIncrement = true;


        public bool IsAutoIncrement(string kind)
        {
            return _autoIncrements.GetSafe(kind,
                GetKeyNames(kind).Count <= 1 && DefaultAutoIncrement);
        }


        public ICollection<string> GetKeyNames(string kind)
        {
            return _names.GetSafe(kind, new[] { DefaultName });
        }


        public object GetKey(string kind, IDictionary<string, object> data)
        {
            var keyNames = GetKeyNames(kind);

            if (keyNames.Count <= 1)
                return data.GetSafe(keyNames.First());

            var key = new CompoundKey();

            foreach (var name in keyNames)
                key[name] = data.GetSafe(name);

            return key;
        }


        public void SetKey(string kind, IDictionary<string, object> data, object key)
        {
            if (key is CompoundKey)
                throw new NotSupportedException();

            var name = GetKeyNames(kind).First();

            if (key == null)
                data.Remove(name);
            else
                data[name] = key;
        }


        public void RegisterKey(string kind, ICollection<string> names, bool? autoIncrement)
        {
            if (names.Count < 1)
                throw new ArgumentException();

            _names[kind] = names;

            if (autoIncrement != null)
                _autoIncrements[kind] = autoIncrement.Value;
        }


        public object PackCompoundKey(string kind, IEnumerable<object> components)
        {
            var result = new CompoundKey();

            foreach (var (item1, item2) in GetKeyNames(kind).Zip(components, Tuple.Create))
                result[item1] = item2;

            return result;
        }
    }
}
