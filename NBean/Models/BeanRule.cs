using System;

namespace NBean.Models
{

    public class BeanRule
    {
        public int Sequence { get; set; }
        public Func<Bean, bool> Test { get; set; }
        public string Message { get; set; }
    }

}
