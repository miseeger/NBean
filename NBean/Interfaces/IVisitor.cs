using System;
using System.Collections.Generic;
using System.Text;

namespace NBean.Interfaces
{

    public interface IVisitor
    {
        T Visit<T>(IVisited visited);
    }

}
