using System;
using NBean.Plugins;
using Xunit;

namespace NBean.Tests
{

    public class SlxKeyProviderTests
    {

        [Fact]
        public void KeyPrefix()
        {
            var kp = new SlxStyleKeyProvider(null);
            Assert.Equal("FOO##", kp.GetKeyPrefix("foo"));
            Assert.Equal("FBR##", kp.GetKeyPrefix("foobar"));
            Assert.Equal("FBRB#", kp.GetKeyPrefix("foobarb"));
            Assert.Equal("FBRBZ", kp.GetKeyPrefix("foobarbaz"));
            Assert.Equal("FBRBZ", kp.GetKeyPrefix("foobarbazetc"));
        }


        [Fact]
        public void InitialKey()
        {
            var kp = new SlxStyleKeyProvider(null);
            Assert.Equal("FOO##-A000000000", kp.GetInitialKey("foo"));
            Assert.Equal("FBR##-A000000000", kp.GetInitialKey("foobar"));
            Assert.Equal("FBRB#-A000000000", kp.GetInitialKey("foobarb"));
            Assert.Equal("FBRBZ-A000000000", kp.GetInitialKey("foobarbaz"));
            Assert.Equal("FBRBZ-A000000000", kp.GetInitialKey("foobarbazetc"));
        }


        [Fact]
        public void NextKey()
        {
            var kp = new SlxStyleKeyProvider(null);
            Assert.Equal("FOO##-A000000001", kp.GetNextKey("FOO##-A000000000"));
            Assert.Equal("FOO##-A00000000A", kp.GetNextKey("FOO##-A000000009"));
            Assert.Equal("FOO##-A000000010", kp.GetNextKey("FOO##-A00000000Z"));
            Assert.Equal("FOO##-A00000001A", kp.GetNextKey("FOO##-A000000019"));
            Assert.Equal("FOO##-A000000020", kp.GetNextKey("FOO##-A00000001Z"));
            Assert.Equal("FOO##-A000000100", kp.GetNextKey("FOO##-A0000000ZZ"));
            Assert.Equal("FOO##-A0000001A0", kp.GetNextKey("FOO##-A00000019Z"));
            Assert.Equal("FOO##-B000000000", kp.GetNextKey("FOO##-AZZZZZZZZZ"));
            Assert.Throws<ArgumentException>(() =>
            {
                kp.GetNextKey("FOO##-ZZZZZZZZZZ");
            });
        }


        [Fact]
        public void FirstBean()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                api.AddObserver(new SlxStyleKeyProvider(api));

                api.Exec("CREATE TABLE Foo (Id, Field)");

                var key = api.Dispense("foo")
                    .Put("Field", "Hello!")
                    .Store();

                var foo = api.Load("foo", key);

                Assert.Equal("FOO##-A000000000", foo["Id"]);
            }
        }


        [Fact]
        public void NewBean()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                api.AddObserver(new SlxStyleKeyProvider(api));

                api.Exec("CREATE TABLE Foo (Id, Field)");
                api.Exec("INSERT INTO Foo VALUES ('FOO##-A00000001A','Hello!')");

                var key = api.Dispense("foo")
                    .Put("Field", "Hello 2!")
                    .Store();

                var foo = api.Load("foo", key);

                Assert.Equal("FOO##-A00000001B", foo["Id"]);
            }
        }

    }

}
