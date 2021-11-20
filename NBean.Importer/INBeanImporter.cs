using System;
using System.Collections.Generic;
using System.Text;

namespace NBean.Importer
{
    internal interface INBeanImporter : IDisposable
    {
        ImportEngine Engine { get; set; }

        bool DoImport(string metaInfo = "");
    }
}
