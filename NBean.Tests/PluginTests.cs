using System;
using NBean.Enums;
using NBean.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace NBean.Tests
{
    public class PluginTests
    {
        private readonly ITestOutputHelper _output;


        public PluginTests(ITestOutputHelper output)
        {
            _output = output;
        }


        [Fact]
        public void RegisterAndInvokeAction()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                api.RegisterAction("MyAction", (aApi, args) =>
                {
                    _output.WriteLine($"Database Type: \"{aApi.DbType}\"");
                    _output.WriteLine($"Parameter: {args[0]}");
                });

                Assert.Equal(PluginType.Action, api.PluginIsRegisteredAs("MyAction"));

                var result = api.Invoke("MyAction", "Param1");

                Assert.True((bool) result);
            }
        }


        [Fact]
        public void RegisterAndInvokeActionFromClass()
        {
            using (var api = SQLitePortability.CreateApi())
            {

                api.RegisterAction("MyAction", PluginCollection.MyAction);

                Assert.Equal(PluginType.Action, api.PluginIsRegisteredAs("MyAction"));

                var result = api.Invoke("MyAction", _output, "Param1");

                Assert.True((bool) result);
            }
        }


        [Fact]
        public void RegisterAndInvokeFunction()
        {
            using (var api = SQLitePortability.CreateApi())
            {

                api.RegisterFunc("MyFunction", (fApi, args) => (int) args[0] * 2);

                Assert.Equal(PluginType.Func, api.PluginIsRegisteredAs("MyFunction"));

                var result = api.Invoke("MyFunction", 2);

                Assert.Equal(4, (int) result);
            }
        }


        [Fact]
        public void RegisterAndInvokeFunctionFromClass()
        {
            using (var api = SQLitePortability.CreateApi())
            {

                api.RegisterFunc("MyFunction", PluginCollection.MyFunction);

                Assert.Equal(PluginType.Func, api.PluginIsRegisteredAs("MyFunction"));

                var result = api.Invoke("MyFunction", 2);

                Assert.Equal(4, (int) result);
            }
        }


        [Fact]
        public void RegisterAndInvokeBeanAction()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                var bean = api.Dispense("TestBean");

                api.RegisterBeanAction("MyBeanAction", (aBean, args) =>
                {
                    _output.WriteLine($"The Bean is of kind: \"{aBean.GetKind()}\"");
                    _output.WriteLine($"Parameter: {args[0]}");
                });

                Assert.Equal(PluginType.BeanAction, api.PluginIsRegisteredAs("MyBeanAction"));

                var result = api.Invoke("MyBeanAction", bean, "Param1");

                Assert.True((bool) result);
            }
        }


        [Fact]
        public void RegisterAndInvokeBeanActionFromClass()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                var bean = api.Dispense("TestBean");

                api.RegisterBeanAction("MyBeanAction", PluginCollection.MyBeanAction);

                Assert.Equal(PluginType.BeanAction, api.PluginIsRegisteredAs("MyBeanAction"));

                var result = api.Invoke("MyBeanAction", bean, _output, "Param1");

                Assert.True((bool) result);
            }
        }


        [Fact]
        public void RegisterAndInvokeBeanActionNoBean()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                api.RegisterBeanAction("MyBeanAction", (aBean, args) => { });

                Assert.Equal(PluginType.BeanAction, api.PluginIsRegisteredAs("MyBeanAction"));
                Assert.Throws<BeanIsMissingException>(() =>
                {
                    var result = api.Invoke("MyBeanAction", "Param1", "Param2");
                });
            }
        }


        [Fact]
        public void RegisterAndInvokeBeanFunction()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                var bean = api.Dispense("TestBean");

                api.RegisterBeanFunc("ReverseBeanKind", (aBean, args) =>
                {
                    var chArr = aBean.GetKind().ToCharArray();
                    Array.Reverse((Array) chArr);
                    return new string(chArr);
                });

                Assert.Equal(PluginType.BeanFunc, api.PluginIsRegisteredAs("ReverseBeanKind"));

                var result = api.Invoke("ReverseBeanKind", bean);

                Assert.Equal("naeBtseT", result.ToString());
            }
        }


        [Fact]
        public void RegisterAndInvokeBeanFunctionFromClass()
        {
            using (var api = SQLitePortability.CreateApi())
            {
                var bean = api.Dispense("TestBean");

                api.RegisterBeanFunc("ReverseBeanKind", PluginCollection.ReverseBeanKind);

                Assert.Equal(PluginType.BeanFunc, api.PluginIsRegisteredAs("ReverseBeanKind"));

                var result = api.Invoke("ReverseBeanKind", bean);

                Assert.Equal("naeBtseT", result.ToString());
            }
        }


        [Fact]
        public void RegisterAndInvokeBeanFuncNoBean()
        {
            using (var api = SQLitePortability.CreateApi())
            {

                api.RegisterBeanFunc("MyBeanFunc", (aBean, args) => null);

                Assert.Equal(PluginType.BeanFunc, api.PluginIsRegisteredAs("MyBeanFunc"));
                Assert.Throws<BeanIsMissingException>(() =>
                {
                    var result = api.Invoke("MyBeanFunc", "Param1", "Param2");
                });
            }
        }

    }
}
