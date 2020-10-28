using System;
using System.Collections.Generic;
using NBean.Models;

namespace NBean.Interfaces
{
    public interface IValidator
    {
        void AddRule(string kind, BeanRule beanRule);
        void AddRules(string kind, IEnumerable<BeanRule> beanRules);
        void ClearRules(string kind);
        void ClearAll();
        Tuple<bool, string> Validate(Bean bean);
    }
}
