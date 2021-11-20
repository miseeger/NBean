using System;

namespace NBean.Importer.Exceptions
{
    public class InvalidPropNameException : Exception
    {
        public InvalidPropNameException() { }

        private InvalidPropNameException(string message) : base(message) { }

        public static InvalidPropNameException Create(string prop)
        {
            var message = $@"The property name '{prop}' is invalid. It contains too many underscores.";
            return new InvalidPropNameException(message);
        }
    }
}
