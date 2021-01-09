using System;

namespace NBean.Exceptions
{
    [Serializable]
    public class NotAnSqlQueryException : Exception
    {
        public NotAnSqlQueryException(string message) : base(message)
        {
        }

        public static NotAnSqlQueryException Create()
        {
            var message = "The SQL command you are about to execute is not an SQL Query. No Result Set to fetch.";
            return new NotAnSqlQueryException(message);
        }

    }
}
