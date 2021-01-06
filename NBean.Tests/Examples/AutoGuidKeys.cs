using System;
using System.Data.SQLite;
using NBean;

namespace LimeBean.Tests.Examples
{

    public class AutoGuidKeys
    {

        public void Scenario()
        {
            using (var api = new BeanApi("data source=:memory:", SQLiteFactory.Instance))
            {
                api.EnterFluidMode();
                api.DefaultKey(false);
                api.AddObserver(new GuidKeyObserver());

                var bean = api.Dispense("foo");
                var key = api.Store(bean);
                Console.WriteLine("Key is: " + key);
            }
        }


        class GuidKeyObserver : BeanObserver
        {
            public override void BeforeStore(Bean bean)
            {
                if (bean["id"] == null)
                    bean["id"] = GenerateGuid();
            }

            static string GenerateGuid()
            {
                return Guid.NewGuid().ToString();
            }
        }
    }

}