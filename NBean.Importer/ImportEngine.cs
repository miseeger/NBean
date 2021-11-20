using NBean.Importer.Exceptions;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace NBean.Importer
{
    public class ImportEngine : IDisposable
    {
        private readonly BeanApi _api;
        private List<string> _props;
        private string _keyProp;
        private string _engineUser;
        private bool _disposed;

        public ImportEngine(BeanApi api, string engineUser = "NBean.ImportEngine")
        {
            _api = api;
            _props = new List<string>();

            _engineUser = engineUser;
        }


        internal void InitProps(List<string> props, string keyProp = "id")
        {
            _keyProp = keyProp;
            _props.Clear();

            foreach (var prop in props)
            {
                var attribute = string.Empty;
                var splitProp = prop.Split('_');

                if (splitProp.Length > 2)
                    throw InvalidPropNameException.Create(prop);

                if (splitProp.Length == 2)
                {
                    if ("XK".Contains(splitProp[1].ToUpper()))
                    {
                        attribute = splitProp[1].ToUpper();
                    }
                    else
                    {
                        throw InvalidPropNameNoAttribException.Create(prop);
                    }
                }

                switch (attribute)
                {
                    case "X":
                        continue;
                    case "K":
                        _keyProp = splitProp[0];
                        break;
                    default:
                        _props.Add(splitProp[0]);
                        break;
                }
            }

            _props.Remove(_keyProp);
        }


        public bool Import(string targetBeanKind, List<string> props, dynamic data)
        {
            if (!_api.IsKnownKind(targetBeanKind))
            {
                throw TableDoesNotExistException.Create(targetBeanKind);
            }

            InitProps(props);

            if (!(_api.GetKeyName(targetBeanKind) == _keyProp || _api.IsKnownKindColumn(targetBeanKind, _keyProp)))
            {
                throw KeyDoesNotExistException.Create(targetBeanKind, _keyProp);
            }

            var apiCurrentUserBak = _api.CurrentUser;
            _api.CurrentUser = _engineUser;

            foreach (var record in data)
            {
                var keyValue = ((ExpandoObject)record).FirstOrDefault(r => r.Key == _keyProp).Value;

                var bean = keyValue == null || (string)keyValue == string.Empty
                    ? _api.Dispense(targetBeanKind)
                    : _api.RowToBean(targetBeanKind,
                        _api.Row($"SELECT * FROM {targetBeanKind} WHERE {_keyProp} = " + "{0}", keyValue)
                      );

                foreach (var prop in _props)
                {
                    var value = ((ExpandoObject)record).FirstOrDefault(r => r.Key == prop).Value;

                    if ((string)value != string.Empty)
                    {
                        bean.Put(prop, value);
                    }
                }

                bean.Store();
            }

            _api.CurrentUser = apiCurrentUserBak;
            return true;
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _props = null;
                }

                _disposed = true;
            }
        }


        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
