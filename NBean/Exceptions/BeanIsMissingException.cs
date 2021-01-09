using System;

namespace NBean.Exceptions
{
    [Serializable]
    public class BeanIsMissingException : Exception
    {
        public BeanIsMissingException(string message) : base(message)
        {
        }

        public static BeanIsMissingException Create(string functionName)
        {
            var message = $"Cannot invoke Bean Function {functionName}. " +
                          "Bean must be provided as first argument.";
            return new BeanIsMissingException(message);
        }

    }
}
