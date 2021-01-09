using System;
using NBean.Enums;

namespace NBean.Exceptions
{
    [Serializable]
    public class PluginAlreadyRegisteredException : Exception
    {
        public PluginAlreadyRegisteredException(string message) : base(message)
        {
        }

        public static PluginAlreadyRegisteredException Create(string pluginName, string registeredAs)
        {
            var message = $"Plugin {pluginName} is already registered as {registeredAs}.";
            return new PluginAlreadyRegisteredException(message);
        }
    }
}
