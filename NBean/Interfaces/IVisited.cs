namespace NBean.Interfaces
{

    public interface IVisited
    {
        T Accept<T>(IVisitor Visitor);
    }

}
