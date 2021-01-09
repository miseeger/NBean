using System;
using System.Collections.Generic;
using System.Linq;
using NBean.Interfaces;

namespace NBean
{
    internal class BeanCrud : IBeanCrud
    {
        private IStorage _storage;
        private ITransactionSupport _transactionSupport;
        private IKeyAccess _keyAccess;
        private IList<BeanObserver> _observers;
        private IBeanFactory _factory;

        public bool DirtyTracking { get; set; }


        public BeanCrud(IStorage storage, ITransactionSupport transactionSupport, IKeyAccess keys, IBeanFactory factory)
        {
            _storage = storage;
            _transactionSupport = transactionSupport;
            _keyAccess = keys;
            _observers = new List<BeanObserver>();
            _factory = factory;
            DirtyTracking = true;
        }


        public void AddObserver(BeanObserver observer)
        {
            if (_observers.Any(loadedObserver => loadedObserver.GetType() == observer.GetType()))
                return;

            _observers.Add(observer);
        }


        public object GetObserver<T>()
        {
            return _observers.FirstOrDefault(o => o.GetType() == typeof(T));
        }


        public void RemoveObserver<T>()
        {
            var observer = _observers.FirstOrDefault(o => o.GetType() == typeof(T));

            if (observer != null)
            {
                _observers.Remove(observer);
            }
        }


        public bool IsObserverLoaded<T>()
        {
            return _observers.Any(o => o.GetType() ==  typeof(T));
        }


        public bool HasObservers()
        {
            return _observers.Any();
        }


        public Bean Dispense(string kind)
        {
            return ContinueDispense(_factory.Dispense(kind));
        }


        public T Dispense<T>() where T : Bean, new()
        {
            return ContinueDispense(_factory.Dispense<T>());
        }


        public Bean RowToBean(string kind, IDictionary<string, object> row)
        {
            return row == null ? null : ContinueLoad(Dispense(kind), row);
        }


        public T RowToBean<T>(IDictionary<string, object> row) where T : Bean, new()
        {
            return row == null ? null : ContinueLoad(Dispense<T>(), row);
        }


        public Bean Load(string kind, object key)
        {
            return RowToBean(kind, _storage.Load(kind, key));
        }


        public T Load<T>(object key) where T : Bean, new()
        {
            return RowToBean<T>(_storage.Load(Bean.GetKind<T>(), key));
        }


        public object Store(Bean bean)
        {
            EnsureDispensed(bean);

            var isNew = _storage.IsNew(bean);

            ImplicitTransaction(() => 
            {
                bean.BeforeStore();

                if (isNew)
                    bean.BeforeInsert();
                else
                    bean.BeforeUpdate();

                foreach (var observer in _observers)
                {
                    observer.BeforeStore(bean);

                    if (isNew)
                        observer.BeforeInsert(bean);
                    else
                        observer.BeforeUpdate(bean);
                }

                var key = _storage.Store(bean.GetKind(), bean.Export(), DirtyTracking ? bean.GetDirtyNames() : null);

                if (key is CompoundKey)
                {
                    // !!! compound keys must not change during insert/update
                }
                else
                {
                    bean.SetKey(_keyAccess, key);
                }

                if (isNew)
                    bean.AfterInsert();
                else
                    bean.AfterUpdate();

                bean.AfterStore();

                foreach (var observer in _observers)
                {
                    if (isNew)
                        observer.AfterInsert(bean);
                    else
                        observer.AfterUpdate(bean);
                    
                    observer.AfterStore(bean);
                }

                return true;
            });

            bean.ForgetDirtyBackup();

            return bean.GetKey(_keyAccess);
        }


        public void Trash(Bean bean)
        {
            EnsureDispensed(bean);

            if (bean.GetKey(_keyAccess) == null)
                return;

            ImplicitTransaction(() => 
            {
                bean.BeforeTrash();

                foreach (var observer in _observers)
                    observer.BeforeTrash(bean);

                _storage.Trash(bean.GetKind(), bean.GetKey(_keyAccess));

                bean.AfterTrash();

                foreach (var observer in _observers)
                    observer.AfterTrash(bean);

                return true;
            });
        }


        private T ContinueDispense<T>(T bean) where T : Bean
        {
            bean.AfterDispense();

            foreach (var observer in _observers)
                observer.AfterDispense(bean);

            return bean;
        }

        private T ContinueLoad<T>(T bean, IDictionary<string, object> row) where T : Bean
        {
            bean.BeforeLoad();

            foreach (var observer in _observers)
                observer.BeforeLoad(bean);

            bean.Import(row);
            bean.ForgetDirtyBackup();

            bean.AfterLoad();

            foreach (var observer in _observers)
                observer.AfterLoad(bean);

            return bean;
        }


        private void ImplicitTransaction(Func<bool> action)
        {
            if (_transactionSupport == null 
                || !_transactionSupport.ImplicitTransactions 
                || _transactionSupport.InTransaction)
                action();
            else
                _transactionSupport.Transaction(action);
        }


        private static void EnsureDispensed(Bean bean)
        {
            if (!bean.Dispensed)
                throw new InvalidOperationException(
                    "Do not instantiate beans directly, use BeanApi.Dispense method instead.");
        }

    }

}
