namespace NBean.Interfaces 
{
    public interface IBeanFactory : IBeanDispenser 
    {
        IBeanOptions Options { get; }
    }

}
