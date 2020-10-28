using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NBean.Interfaces;
using NBean.Models;

namespace NBean
{
    
    public class Validator : IValidator
    {

        private readonly Dictionary<string, List<BeanRule>> _beanRules;


        public Validator()
        {
            _beanRules = new Dictionary<string, List<BeanRule>>();
        }


        public Validator(Dictionary<string, List<BeanRule>> beanRules)
        {
            _beanRules = beanRules;
        }


        public void AddRule(string kind, BeanRule beanRule)
        {
            if (!_beanRules.ContainsKey(kind))
            {
                _beanRules.Add(kind, new List<BeanRule>{ beanRule });
            }
            else
            {
                _beanRules[kind].Add(beanRule);
            }
        }


        public void AddRules(string kind, IEnumerable<BeanRule> beanRules)
        {
            if (!_beanRules.ContainsKey(kind))
            {
                _beanRules.Add(kind, new List<BeanRule>(beanRules));
            }
            else
            {
                foreach (var beanRule in beanRules)
                {
                    _beanRules[kind].Add(beanRule);
                }
            }
        }


        public void ClearRules(string kind)
        {
            if (_beanRules.ContainsKey(kind))
            {
                _beanRules[kind].Clear();
            }
        }


        public void ClearAll()
        {
            _beanRules.Clear();
        }


        public Dictionary<string, List<BeanRule>> GetRules()
        {
            return _beanRules;
        }


        public List<BeanRule> GetRules(string kind)
        {
            return _beanRules.ContainsKey(kind) 
                ? _beanRules[kind].ToList()
                : new List<BeanRule>();
        }


        public Tuple<bool, string> Validate(Bean bean)
        {
            var kind = bean.GetKind();

            if (!_beanRules.ContainsKey(kind))
                return
                    new Tuple<bool, string>(true, string.Empty);
            
            var failures = 
                _beanRules[kind]
                    .OrderBy(br => br.Sequence)
                    .Where(br => br.Test(bean) == false)
                    .Aggregate(new StringBuilder(), 
                        (sb, br) => sb.AppendLine(br.Message), sb => sb.ToString()
                    );
            return 
                new Tuple<bool, string>(failures == string.Empty, failures);
        }
        
    }

}
