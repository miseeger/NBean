using System;

namespace NBean.Importer.Exceptions
{
    public class KeyDoesNotExistException : Exception
    {
        public KeyDoesNotExistException() { }

        private KeyDoesNotExistException(string message) : base(message) { }

        public static KeyDoesNotExistException Create(string tableName, string pkName)
        {
            var message = $@"The primary key field '{pkName}' does not exist in table '{tableName}'.";
            return new KeyDoesNotExistException(message);
        }
    }
}
