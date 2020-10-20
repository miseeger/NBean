using System;
using Xunit.Abstractions;

namespace NBean.Tests
{
    
    public static class PluginCollection
    {
        
        public static void MyAction(BeanApi bApi, params object[] args)
        {
            var output = (ITestOutputHelper)args[0];

            output.WriteLine($"Database Type: \"{bApi.DbType}\"");
            output.WriteLine($"Parameter: {args[1]}");
        }


        public static object MyFunction(BeanApi bApi, params object[] args)
        {
            return (int) args[0] * 2;
        }


        public static void MyBeanAction(Bean aBean, params object[] args)
        {
            var output = (ITestOutputHelper)args[0];

            output.WriteLine($"The Bean is of kind: \"{aBean.GetKind()}\"");
            output.WriteLine($"Parameter: {args[1]}");
        }


        public static object ReverseBeanKind(Bean aBean, params object[] args)
        {
            var chArr = aBean.GetKind().ToCharArray();
            Array.Reverse((Array) chArr);
            return new string (chArr);
        }

    }

}
