using System;

namespace NBean.Exceptions
{
    [Serializable]
    public class BeanIsMissingException : Exception
    {
        public BeanIsMissingException(string message) : base(message)
        {
        }
    }
}