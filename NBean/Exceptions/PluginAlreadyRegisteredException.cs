using System;

namespace NBean.Exceptions
{
    [Serializable]
    public class PluginAlreadyRegisteredException : Exception
    {
        public PluginAlreadyRegisteredException(string message) : base(message)
        {
        }
    }
}