using System;

namespace NBean.Exceptions
{
    [Serializable]
    public class NotExecutableException : Exception
    {
        public NotExecutableException()
        {
        }

        public NotExecutableException(string message) : base(message)
        {
        }

        public static NotExecutableException Create()
        {
            var message = "The SQL command you are about to execute is an SQL Query.";
            return new NotExecutableException(message);
        }

    }
}