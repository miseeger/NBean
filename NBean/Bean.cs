using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NBean.Interfaces;

namespace NBean
{
    public partial class Bean : IBean
    {
        private readonly IDictionary<string, object> _props = new Dictionary<string, object>();
        private IDictionary<string, object> _dirtyBackup;

        private static readonly ConcurrentDictionary<Type, string> _kindCache = new ConcurrentDictionary<Type, string>();
        private readonly string _kind;

        internal bool Dispensed;

        internal BeanApi Api;


        internal Bean() { }


        protected internal Bean(string kind)
        {
            _kind = kind;
        }


        /// <summary>
        /// Get the Kind (Table Name) of the Bean
        /// </summary>
        /// <returns>Table name</returns>
        public string GetKind()
        {
            return _kind;
        }


        /// <summary>
        /// Get the Kind (Table Name) of the Bean type
        /// </summary>
        /// <returns>Table name</returns>
        public static string GetKind<T>() where T : Bean, new()
        {
            return _kindCache.GetOrAdd(typeof(T), type => new T().GetKind());
        }

        public override string ToString()
        {
            return _kind ?? base.ToString();
        }


        internal object GetKey(IKeyAccess access)
        {
            return access.GetKey(_kind, _props);
        }


        internal void SetKey(IKeyAccess access, object key)
        {
            access.SetKey(_kind, _props, key);
        }


        protected BeanApi GetApi()
        {
            return Api;
        }


        // ----- Accessors ----------------------------------------------------

        /// <summary>
        /// Get or Set the value of a Column
        /// </summary>
        /// <param name="name">Name of the Column to Get or Set</param>
        public object this[string name]
        {
            get
            {
                if (ValidateGetColumns)
                    ValidateColumnExists(name);

                return _props.GetSafe(name);
            }
            set
            {
                SaveDirtyBackup(name, value);
                _props[name] = value;
            }
        }


        /// <summary>
        /// Get the value of a Column in a given Type
        /// </summary>
        /// <typeparam name="T">Type of the return value</typeparam>
        /// <param name="name">Name of the Column to Get</param>
        /// <returns>Value of the requested Column as type T</returns>
        public T Get<T>(string name)
        {
            return this[name].ConvertSafe<T>();
        }


        /// <summary>
        /// Set the value of a Column
        /// </summary>
        /// <param name="name">Name of the Column to Set</param>
        /// <param name="value">Value to Set the Column to</param>
        public Bean Put(string name, object value)
        {
            this[name] = value;
            return this;
        }


        /// <summary>
        /// Sets or overrides Bean properties from a given Dictionary
        /// </summary>
        /// <param name="data"></param>
        /// <returns>Current Bean instance.</returns>
        public Bean Put(Dictionary<string, object> data)
        {
            Import(data);
            return this;
        }


        // ----- Actions ------------------------------------------------------

        /// <summary>
        /// Stores the current Bean in database.
        /// </summary>
        /// <returns>Primary key of the bean.</returns>
        public object Store()
        {
            return Api.Store(this);
        }


        /// <summary>
        /// Deletes the current Bean from database
        /// </summary>
        public void Trash()
        {
            Api.Trash(this);
        }


        // ----- Bean Options -------------------------------------------------

        internal bool ValidateGetColumns
        {
            get;
            set;
        }


        public bool ColumnExists(string name)
        {
            return _props.ContainsKey(name);
        }

        private void ValidateColumnExists(string name)
        {
            if (ColumnExists(name) == false)
                throw Exceptions.ColumnNotFoundException.New(this, name);
        }


        // ----- Import / Export ----------------------------------------------

        public IDictionary<string, object> Export()
        {
            return new Dictionary<string, object>(_props);
        }


        public void Import(IDictionary<string, object> data)
        {
            foreach (var entry in data)
                this[entry.Key] = entry.Value;
        }


        /// <summary>
        /// Retrieve the name of each Column held in this Bean
        /// </summary>
        public IEnumerable<string> Columns => _props.Keys;


        /// <summary>
        /// Gets the Data portion of this Bean.
        /// </summary>
        public IDictionary<string, object> Data => Export();


        // ----- Keys ---------------------------------------------------------

        /// <summary>
        /// Gets the name of the non compound key of the bean
        /// </summary>
        /// <returns>Key name</returns>
        public string GetKeyName()
        {
            return Api.GetKeyName(this.GetKind());
        }


        /// <summary>
        /// Gets the value of the non compound key of the bean
        /// </summary>
        /// <returns>Key value</returns>
        public object GetKeyValue()
        {
            return Api.GetNcKeyValue(this);
        }


        // ----- Dirty tracking -----------------------------------------------

        private void SaveDirtyBackup(string name, object newValue)
        {
            var currentValue = _props.GetSafe(name);

            if (Equals(newValue, currentValue))
                return;

            var initialValue = currentValue;

            if (_dirtyBackup != null && _dirtyBackup.ContainsKey(name))
                initialValue = _dirtyBackup[name];

            if (Equals(newValue, initialValue))
            {
                _dirtyBackup?.Remove(name);
            }
            else
            {
                if (_dirtyBackup == null)
                    _dirtyBackup = new Dictionary<string, object>();
                _dirtyBackup[name] = currentValue;
            }
        }


        internal void ForgetDirtyBackup()
        {
            _dirtyBackup = null;
        }

        
        internal IDictionary<string, object> GetDirtyBackup()
        {
            return _dirtyBackup;
        }


        internal ICollection<string> GetDirtyNames()
        {
            if (_dirtyBackup == null)
                return new string[0];

            return new HashSet<string>(_dirtyBackup.Keys);
        }


        // ----- Hooks --------------------------------------------------------

        protected internal virtual void AfterDispense() { }

        protected internal virtual void BeforeLoad() { }

        protected internal virtual void AfterLoad() { }

        protected internal virtual void BeforeStore() { }
        protected internal virtual void BeforeInsert() { }
        protected internal virtual void BeforeUpdate() { }

        protected internal virtual void AfterStore() { }
        protected internal virtual void AfterInsert() { }
        protected internal virtual void AfterUpdate() { }

        protected internal virtual void BeforeTrash() { }

        protected internal virtual void AfterTrash() { }
    }

}
