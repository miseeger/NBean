namespace NBean.Interfaces
{
    public interface IObserverSupport
    {
        void AddObserver(BeanObserver observer);
        object GetObserver<T>();
        void RemoveObserver<T>();
        bool IsObserverLoaded<T>();
        bool HasObservers();
    }
}
