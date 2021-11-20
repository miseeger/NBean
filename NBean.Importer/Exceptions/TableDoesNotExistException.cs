using System;

namespace NBean.Importer.Exceptions
{
    public class TableDoesNotExistException : Exception
    {
        public TableDoesNotExistException() { }

        private TableDoesNotExistException(string message) : base(message) { }

        public static TableDoesNotExistException Create(string tableName)
        {
            var message = $@"The table '{tableName}' does not exist in the database";
            return new TableDoesNotExistException(message);
        }
    }
}
