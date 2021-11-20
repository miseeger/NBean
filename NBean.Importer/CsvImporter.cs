using System;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;

namespace NBean.Importer
{
    public class CsvImporter : INBeanImporter
    {
        public ImportEngine Engine { get; set; }

        private readonly string _delimiter;
        private bool disposed;


        public CsvImporter(BeanApi api, string delimiter = ";", string currentUser = "CsvImporter")
        {
            _delimiter = delimiter;
            Engine = new ImportEngine(api, currentUser);
        }


        public bool DoImport(string metaInfo = "")
        {
            var filename = metaInfo;
            var importFolder = Path.GetDirectoryName(filename);
            var processedFolder = Path.Combine(importFolder, "processed");

            if (!Directory.Exists(processedFolder))
            {
                Directory.CreateDirectory(processedFolder);
            }

            using var reader = new StreamReader(filename);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = _delimiter,
                PrepareHeaderForMatch = args =>
                    args.Header.Contains(":")
                        ? args.Header.Split(':')[0]
                        : args.Header
            };

            using var csv = new CsvReader(reader, config);

            csv.Read();
            csv.ReadHeader();

            var targetBeanKind = Path.GetFileNameWithoutExtension(filename).Split('_')[0];
            var props = csv.HeaderRecord.ToList();
            var data = csv.GetRecords<dynamic>().ToList();

            reader.Close();

            if (Engine.Import(targetBeanKind, props, data))
            {
                File.Move(filename, Path.Combine(processedFolder,
                    $"{DateTime.Now:yyyy-MM-ddTHH-mm-ss-FFF}_{Path.GetFileName(filename)}"));
            }

            return true;
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Engine.Dispose();
                }

                disposed = true;
            }
        }


        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
