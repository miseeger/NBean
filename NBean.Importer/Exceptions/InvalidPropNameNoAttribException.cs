using System;

namespace NBean.Importer.Exceptions
{
    public class InvalidPropNameNoAttribException : Exception
    {
        public InvalidPropNameNoAttribException() { }

        private InvalidPropNameNoAttribException(string message) : base(message) { }

        public static InvalidPropNameNoAttribException Create(string prop)
        {
            var message = $@"The property name '{prop}' is invalid. It has no valid attribute prefix (_X or _K).";
            return new InvalidPropNameNoAttribException(message);
        }
    }
}
