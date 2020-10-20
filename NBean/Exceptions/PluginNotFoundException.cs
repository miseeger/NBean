using System;

namespace NBean.Exceptions
{
    [Serializable]
    public class PluginNotFoundException : Exception
    {
        public PluginNotFoundException(string message) : base(message)
        {
        }
    }
}