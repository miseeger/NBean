using System;

namespace NBean.Exceptions 
{
    class MissingForeignKeyColumnException : Exception 
    {
        public MissingForeignKeyColumnException(string message) : base(message)
        {
        }

        public static MissingForeignKeyColumnException Create(string kind, string column) 
        {
            var message = $@"The foreign key column '{column}' is missing in Bean '{kind}'.";
            return new MissingForeignKeyColumnException(message);
        }
    }
}
