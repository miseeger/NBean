using System;
using System.Collections.Generic;
using System.Linq;
using NBean.Interfaces;

namespace NBean
{
    class KeyUtil : IKeyAccess
    {
        private readonly IDictionary<string, IReadOnlyList<string>> _names =
            new Dictionary<string, IReadOnlyList<string>>();
        private readonly IDictionary<string, bool> _autoIncrements =
            new Dictionary<string, bool>();

        public string DefaultName = "id";
        public bool DefaultAutoIncrement = true;
        public bool AutoIncrementReplaced = false;


        public bool IsAutoIncrement(string kind)
        {
            return _autoIncrements.GetSafe(kind,
                GetKeyNames(kind).Count <= 1 && DefaultAutoIncrement);
        }


        public bool IsAutoIncrementReplaced()
        {
            return AutoIncrementReplaced;
        }


        public IReadOnlyList<string> GetKeyNames(string kind)
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


        public void RegisterKey(string kind, IReadOnlyList<string> names, bool? autoIncrement)
        {
            if (names.Count < 1)
                throw new ArgumentException();

            _names[kind] = names;

            if (autoIncrement != null)
                _autoIncrements[kind] = autoIncrement.Value;
        }


        public object PackCompoundKey(string kind, IReadOnlyList<object> components)
        {
            var result = new CompoundKey();
            var names = GetKeyNames(kind);

            for (var i = 0; i < components.Count; i++)
                result[names[i]] = components[i];

            return result;
        }
    }
}
