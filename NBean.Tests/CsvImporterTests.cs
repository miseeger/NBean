using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Moq;
using NBean.Importer;
using NBean.Plugins;
using Xunit;

namespace NBean.Tests
{

    public class CsvImporterTests : IDisposable
    {
        private readonly BeanApi _api;
        private readonly CsvImporter _importer;
        private readonly string _importFolder;

        public CsvImporterTests()
        {
            _api = SQLitePortability.CreateApi();
            _api.AddObserver(new SlxStyleKeyProvider(_api));
            _importFolder = @".\Import";
            _importer = new CsvImporter(_api);
        }


        public void Dispose()
        {
            _api.Dispose();
        }


        private void CreateImportTestScenario()
        {
            _api.Exec("CREATE TABLE CostumeCategory (id VARCHAR(16) NOT NULL PRIMARY KEY, " +
                "LegionId INT, Name VARCHAR(128), Prefix VARCHAR(16))");
        }


        [Fact]
        public void ImportsCostumeCategories()
        {
            CreateImportTestScenario();

            _importer.DoImport(Path.Combine(_importFolder, "CostumeCategory.csv"));

            var costumeCategoryCount = _api.Count("CostumeCategory");

            Assert.Equal(27, costumeCategoryCount);
        }


        [Fact]
        public void UpdatesCostumeCategoryByDefaultId()
        {
            CreateImportTestScenario();

            _importer.DoImport(Path.Combine(_importFolder, "CostumeCategory_1.csv"));
            _importer.DoImport(Path.Combine(_importFolder, "CostumeCategory_upd.csv"));

            var cC = _api.Load("CostumeCategory", "CSTMC-A000000000");

            Assert.Equal("Stormtooper...Changed", cC["Name"]);
        }


        [Fact]
        public void UpdatesCostumeCategoryByLegionId()
        {
            CreateImportTestScenario();

            _importer.DoImport(Path.Combine(_importFolder, "CostumeCategory_2.csv"));
            _importer.DoImport(Path.Combine(_importFolder, "CostumeCategory_upd2.csv"));
            _api.Key("CostumeCategory", "LegionId", false);

            var cC = _api.Load("CostumeCategory", 2);

            Assert.Equal("Sandtrooper...Changed", cC["Name"]);
        }
    }

}
