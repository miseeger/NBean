using System;

namespace NBean.Exceptions
{
    [Serializable]
    public class CannotMapIEnumerableException : Exception
    {
        public CannotMapIEnumerableException()
        {
        }

        public CannotMapIEnumerableException(string message) : base(message)
        {
        }

        public static CannotMapIEnumerableException Create()
        {
            var message = $"Cannot map an IEnumerable to Bean.";
            return new CannotMapIEnumerableException(message);
        }

    }
}
