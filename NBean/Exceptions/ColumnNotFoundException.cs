using System;

namespace NBean.Exceptions 
{
    class ColumnNotFoundException : Exception 
    {
        public ColumnNotFoundException(string message) : base(message)
        {
        }
        
        public static ColumnNotFoundException Create(Bean bean, string column) 
        {
            var message = $@"The requested column '{column}' for Bean '{bean.GetKind()}' was not found. "
                + "You can assign a value to the column to create it";
            return new ColumnNotFoundException(message);
        }
    }
}
