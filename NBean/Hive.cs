using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NBean.Interfaces;

namespace NBean
{
    public class Hive: IHive
    {
        private readonly Dictionary<string, object> _hiveDict;


        public Hive()
        {
            _hiveDict = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);
        }


        public object this[string key]
        {
            get => _hiveDict.TryGetValue(key, out var result) ? result : null;

            set
            {
                if (Exists(key))
                {
                    _hiveDict[key] = value;
                }
                else
                {
                    _hiveDict.Add(key, value);
                }

            }
        }


        public void Clear(string key)
        {
            _hiveDict[key] = null;
        }


        public void ClearAll()
        {
            var keys = _hiveDict.Keys.ToList();

            foreach (var key in keys)
            {
                _hiveDict[key] = null;
            }
        }


        public void Delete(string key)
        {
            _hiveDict.Remove(key);
        }


        public void DeleteAll()
        {
            _hiveDict.Clear();
        }


        public bool Exists(string key)
        {
            return _hiveDict.Any(e => (e.Key == key));
        }

	}
}