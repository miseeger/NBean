using System;

namespace NBean.Exceptions 
{
    class LinkAlreadyExistsException : Exception 
    {
        public LinkAlreadyExistsException(string message) : base(message)
        {
        }

        public static LinkAlreadyExistsException New(string kind1, string kind2) 
        {
            var message = $@"The Link to be established between {kind1} and {kind2} already Exists.";
            return new LinkAlreadyExistsException(message);
        }
    }
}
