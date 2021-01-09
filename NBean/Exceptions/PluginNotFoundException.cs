using System;

namespace NBean.Exceptions
{
    [Serializable]
    public class PluginNotFoundException : Exception
    {
        public PluginNotFoundException(string message) : base(message)
        {
        }

        public static PluginNotFoundException Create(string pluginName)
        {
            var message = $"Plugin {pluginName} could not be found.";
            return new PluginNotFoundException(message);
        }
    }
}
