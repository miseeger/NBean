using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NBean
{
    internal class CompoundKey
    {
        private readonly IDictionary<string, object> _components = new Dictionary<string, object>();

        public object this[string component]
        {
            get => _components.ContainsKey(component) ? _components[component] : null;
            set => _components[component] = value ?? throw new ArgumentNullException(component);
        }


        public override string ToString()
        {
            return string.Join(", ", _components.OrderBy(e => e.Key).Select(c => c.Key + "="
                + Convert.ToString(c.Value, CultureInfo.InvariantCulture)));
        }
    }
}
