using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using NBean.Exceptions;
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
                throw Exceptions.ColumnNotFoundException.Create(this, name);
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


        // ----- Relations ----------------------------------------------------

        internal string GetFkName(string kind)
        {
            return $"{kind}_{Api.DefaultKey()}";
        }


        /// <summary>
        /// Gets a List of Beans that are owned by the current Bean in a
        /// manner of a 1:n reference.
        /// </summary>
        /// <param name="ownedKind">Kind of Bean that references to this Bean via Foreign Key.</param>
        /// <returns>List of owned Beans.</returns>
        public IList<Bean> GetOwnedList(string ownedKind)
        {
            return Api.Find(ownedKind, "WHERE " + GetFkName(GetKind()) + " = {0}", GetKeyValue()).ToList();
        }


        /// <summary>
        /// Attaches a Bean to this bean in a manner of a 1:n reference. The Bean to attach
        /// has the Foreign key that references to this Bean's Id.
        /// </summary>
        /// <param name="bean">Bean to be attached. It references the current Bean via Foreign Key.</param>
        /// <returns>true, if successful</returns>
        public bool AttachOwned(Bean bean)
        {
            var foreignKey = GetFkName(GetKind());
            var relatingKind = bean.GetKind();

            if (!Api.IsKnownKindColumn(relatingKind, foreignKey))
                throw MissingForeignKeyColumnException.Create(relatingKind, foreignKey);

            bean
                .Put(foreignKey, GetKeyValue())
                .Store();

            return true;
        }


        /// <summary>
        /// Attaches a list of Beans to the current Bean in a manner of a 1:n reference.
        /// </summary>
        /// <param name="beans">Beans to be attached </param>
        /// <returns>true, if successful</returns>
        public bool AttachOwned(IList<Bean> beans)
        {
            foreach (var bean in beans)
            {
                AttachOwned(bean);
            }

            return true;
        }


        /// <summary>
        /// Detaches a referencing Bean from the current Bean. The referencing bean
        /// may be trashed (deleted) or retained in the table but then as orphaned Bean. 
        /// </summary>
        /// <param name="bean">Bean to be detached.</param>
        /// <param name="trashOwned">Delete or retain Bean as orphaned Bean.</param>
        /// <returns>true, if successful</returns>
        public bool DetachOwned(Bean bean, bool trashOwned = false)
        {
            if (trashOwned)
            {
                bean.Trash();
            }
            else
            {
                var foreignKey = GetFkName(GetKind());

                if (!Api.IsKnownKindColumn(bean.GetKind(), foreignKey))
                    throw MissingForeignKeyColumnException.Create(GetKind(), foreignKey);

                bean
                    .Put(foreignKey, null)
                    .Store();
            }

            return true;
        }


        /// <summary>
        /// Detaches a list of Beans from the current Bean. The referencing beans
        /// may be deleted or retained as orphaned Beans.
        /// </summary>
        /// <param name="beans">List of Beans</param>
        /// <param name="trashOwned">Delete or retain als orphaned Beans.</param>
        /// <returns>true, if successful</returns>
        public bool DetachOwned(IList<Bean> beans, bool trashOwned = false)
        {
            foreach (var bean in beans)
            {
                DetachOwned(bean, trashOwned);
            }

            return true;
        }


        /// <summary>
        /// Gets the owner Bean (the "1"-side of a 1:n relation
        /// </summary>
        /// <param name="ownerKind">Kind of the owner Bean.</param>
        /// <returns>Owner Bean</returns>
        public Bean GetOwner(string ownerKind)
        {
            var foreignKey = GetFkName(ownerKind);

            return Api.Load(ownerKind, _props[foreignKey]);
        }


        /// <summary>
        /// Attaches an owner Bean in a Manner of a 1:n relation. The owner
        /// represents the "1"-side of this relation.
        /// </summary>
        /// <param name="bean">Owner Bean</param>
        /// <returns>true, if successful</returns>
        public bool AttachOwner(Bean bean)
        {
            var foreignKey = GetFkName(bean.GetKind());

            if (!Api.IsKnownKindColumn(GetKind(), foreignKey))
                throw MissingForeignKeyColumnException.Create(GetKind(), foreignKey);

            Put(foreignKey, bean.GetKeyValue());
            Store();

            return true;
        }


        /// <summary>
        /// Detaches the owner ("1"-side of a 1:n relation) from the current Bean.
        /// Only the owner Kind is needed, here. The current (owned or "n"-side) Bean
        /// may be deleted or retained as orphaned Bean.
        /// </summary>
        /// <param name="ownerKind">Name of the owner Bean.</param>
        /// <param name="trashOwned">Deletes the current Bean when owner is detached</param>
        /// <returns></returns>
        public bool DetachOwner(string ownerKind, bool trashOwned = false)
        {
            if (trashOwned)
            {
                Trash();
            }
            else
            {
                var foreignKey = GetFkName(ownerKind);

                if (!Api.IsKnownKindColumn(GetKind(), foreignKey))
                    throw MissingForeignKeyColumnException.Create(GetKind(), foreignKey);

                Put(foreignKey, null);
                Store();
            }

            return true;
        }


        internal LinkScenario GetLinkScenario(string linkedKind)
        {
            var ls = new LinkScenario()
            {
                // referencing Bean (m Bean)
                LinkingKind = GetKind(),

                // referenced Bean (n Bean)
                LinkedKind = linkedKind
            };

            // linking Bean (m:n Bean)
            ls.LinkKind = Api.GetLinkName(ls.LinkingKind, ls.LinkedKind);

            if (ls.LinkedKind == string.Empty)
                return ls;

            // referencing Bean (m Bean)
            ls.LinkingKindPkValue = GetKeyValue();
            ls.LinkingKindFkName = GetFkName(ls.LinkingKind);

            // referenced Bean (n Bean)
            ls.LinkedKindPkName = Api.GetKeyName(ls.LinkedKind);
            ls.LinkedKindFkName = GetFkName(ls.LinkedKind);

            // linking Bean (m:n Bean)
            ls.LinkKindPkName = Api.GetKeyName(ls.LinkKind);

            return ls;
        }


        internal string CreateLinkQuery(string projection, LinkScenario ls)
        {
            return 
                "SELECT \r\n" +
                $"    {projection} \r\n" +
                "FROM \r\n" +
                $"   {Api.GetQuoted(ls.LinkKind)} link \r\n" +
                $"   JOIN {Api.GetQuoted(ls.LinkedKind)} bean ON (bean.id = link.{Api.GetQuoted(ls.LinkedKindFkName)}) \r\n" +
                "WHERE \r\n" +
                $"    link.{Api.GetQuoted(ls.LinkingKindFkName)} = " + "{0}";
        }


        /// <summary>
        /// Gets the Beans of a given Kind that are linked to the current Bean in
        /// a m:n relational manner.
        /// </summary>
        /// <param name="kind">Linked Kind of Bean.</param>
        /// <returns>List of linked Beans.</returns>
        public IList<Bean> GetLinkedList(string kind)
        {
            var result = new List<Bean>();

            var ls = GetLinkScenario(kind);

            var linkedBeanRows = Api.Rows(true, CreateLinkQuery("bean.*", ls), ls.LinkingKindPkValue);

            foreach (var linkedBeanRow in linkedBeanRows)
            {
                var linkedBean = Api.CreateRawBean(kind);
                linkedBean.Import(linkedBeanRow);
                result.Add(linkedBean);
            }

            return result;
        }


        /// <summary>
        /// Gets the Beans of a given Kind that are linked to the current Bean in
        /// a m:n relational manner. In addition the Link Bean (m:n relation Bean)
        /// is returned with its linked Bean.
        /// </summary>
        /// <param name="kind">Linked Kind of Bean.</param>
        /// <returns>List of linked Beans and their Link Bean.</returns>
        public Dictionary<Bean, Bean> GetLinkedListEx(string kind)
        {
            var result = new Dictionary<Bean, Bean>();

            var ls = GetLinkScenario(kind);

            // SELECT * is not an option here, because one Primary Key column is
            // not delivered. So the projection has to be put together "manually"
            // and checked if all Primary keys included.

            var beanProjection = string.Join(", ",
                Api.GetKindColumns(kind).Select(c => $"bean.{c} AS bean_{c}"));

            if (!beanProjection.Contains($"bean.{ls.LinkedKindPkName}"))
                beanProjection = $"bean.{ls.LinkedKindPkName} AS bean_{ls.LinkedKindPkName}, " + beanProjection;

            var linkProjection = string.Join(", ",
                Api.GetKindColumns(ls.LinkKind).Select(c => $"link.{c} AS link_{c}"));

            if (!linkProjection.Contains($"link.{ls.LinkKindPkName}"))
                linkProjection = $"link.{ls.LinkKindPkName} AS link_{ls.LinkKindPkName}, " + linkProjection;

            var linkedBeanRows = Api.Rows(true, 
                CreateLinkQuery($"{beanProjection}, {linkProjection}", ls), ls.LinkingKindPkValue);

            foreach (var linkedBeanRow in linkedBeanRows)
            {
                var linkedBean = Api.CreateRawBean(kind);
                var linkBean = Api.CreateRawBean(ls.LinkKind);

                foreach (var lbr in linkedBeanRow)
                {
                    if (lbr.Key.StartsWith("bean_"))
                        linkedBean.Put(lbr.Key.Replace("bean_", string.Empty), lbr.Value);
                    else
                        linkBean.Put(lbr.Key.Replace("link_", string.Empty), lbr.Value);
                }

                result.Add(linkedBean, linkBean);
            }

            return result;
        }


        /// <summary>
        /// Links the current Bean with another Bean in a m:n relational manner and
        /// provides data (linkProps) for the Link.
        /// </summary>
        /// <param name="bean">Bean to be linked.</param>
        /// <param name="linkProps">Dictionary of Link Properties.</param>
        /// <returns>true, if successful</returns>
        public bool LinkWith(Bean bean, IDictionary<string, object> linkProps = null)
        {
            var ls = GetLinkScenario(bean.GetKind());

            var linkedKindPkValue = bean.GetKeyValue();

            var linkBean = Api.FindOne(false, ls.LinkKind, 
                "WHERE " + ls.LinkingKindFkName + " = {0} AND " + ls.LinkedKindFkName + " = {1}",
                ls.LinkingKindPkValue, linkedKindPkValue);

            if (linkBean != null)
                throw LinkAlreadyExistsException.New(ls.LinkingKind, ls.LinkedKind);

            linkBean = Api.Dispense(ls.LinkKind);

            if (linkProps != null)
                linkBean.Import(linkProps);

            linkBean
                .Put(ls.LinkingKindFkName, ls.LinkingKindPkValue)
                .Put(ls.LinkedKindFkName, linkedKindPkValue)
                .Store();

            return true;
        }


        // <summary>
        /// Unlinks a Bean from the current Bean when they are related in a m:n relational way.
        /// </summary>
        /// <param name="bean">Bean to be unlinked.</param>
        /// <returns>true, if successful</returns>
        public bool Unlink(Bean bean)
        {
            var ls = GetLinkScenario(bean.GetKind());

            var linkBean = Api.FindOne(false, ls.LinkKind,
                "WHERE " + ls.LinkingKindFkName + " = {0} AND " + ls.LinkedKindFkName + " = {1}",
                ls.LinkingKindPkValue, bean.GetKeyValue());

            linkBean?.Trash();

            return true;
        }


        /// <summary>
        /// Links a List of Beans with the current Bean in an m:n relational way and may
        /// provide data to the Link Bean.
        /// </summary>
        /// <param name="beans">Beans to be linked</param>
        /// <param name="linkProps">Dictionary of Link Properties. The are stored with each
        /// established Link.</param>
        /// <returns>true, if successful</returns>
        public bool LinkWith(IList<Bean> beans, IDictionary<string, object> linkProps = null)
        {
            foreach (var bean in beans)
            {
                LinkWith(bean, linkProps);
            }

            return true;
        }


        /// <summary>
        /// Links a List of Beans from the current Bean in an m:n relational way.
        /// </summary>
        /// <param name="beans">Beans to be unlinked</param>
        /// <returns>true, if successful</returns>
        public bool Unlink(IList<Bean> beans)
        {
            foreach (var bean in beans)
            {
                Unlink(bean);
            }

            return true;
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
