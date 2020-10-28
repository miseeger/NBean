using System;

namespace NBean.Models
{

    public class BeanRule
    {
        public Func<Bean, bool> Test { get; set; }
        public string Message { get; set; }
    }

}
