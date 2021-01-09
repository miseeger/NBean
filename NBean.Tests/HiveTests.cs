using Xunit;

namespace NBean.Tests
{

    public class HiveTests
    {

        [Fact]
        public void Indexer()
        {
            var hive = new Hive
            {
                ["MyProp"] = "Value"
            };

            Assert.Null(hive["YourProp"]);
            Assert.Equal("Value", hive["MyProp"]);

            hive["MyProp"] = "NewValue";
            Assert.Equal("NewValue", hive["MyProp"]);
        }

        [Fact]
        public void ClearsAndClearsAll()
        {
            var hive = new Hive
            {
                ["MyProp"] = "Value",
                ["OtherProp"] = "OtherValue"
            };

            hive.Clear("MyProp");
            Assert.Null(hive["MyProp"]);
            Assert.Equal("OtherValue", hive["OtherProp"]);

            hive["MyProp"] = "NewValue";
            hive.ClearAll();
            Assert.Null(hive["MyProp"]);
            Assert.Null(hive["OtherProp"]);
        }

        [Fact]
        public void DeletesAndDeletesAll()
        {
            var hive = new Hive
            {
                ["MyProp"] = "Value",
                ["OtherProp"] = "OtherValue"
            };

            hive.Delete("MyProp");
            Assert.False(hive.Exists("MyProp"));
            Assert.True(hive.Exists("OtherProp"));

            hive["MyProp"] = "ValueRevived";
            hive.DeleteAll();
            Assert.False(hive.Exists("MyProp"));
            Assert.False(hive.Exists("OtherProp"));
        }

    }

}
