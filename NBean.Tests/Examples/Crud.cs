using System;
using System.Data.SQLite;
using NBean;

namespace LimeBean.Tests.Examples
{

    public class Crud
    {

        public void Scenario()
        {
            // Tell about your database
            var r = new BeanApi("data source=:memory:", SQLiteFactory.Instance);

            // Enable automatic schema update
            r.EnterFluidMode();

            // create a bean
            var bean = r.Dispense("person");

            // it's of kind "person"
            Console.WriteLine(bean.GetKind());

            // give me the Id of the newly stored bean
            var id = bean
            // fill it
                .Put("name", "Alex")
                .Put("year", 1984)
                .Put("smart", true)
            // store it
                .Store();               

            // Database schema will be updated automatically for you

            // Now the bean has an id
            Console.WriteLine(bean["id"]);

            // load a bean
            bean = r.Load("person", id);

            bean
            // change it
                .Put("name", "Lexa")
                .Put("new_prop", 123)
            // commit changes
                .Store();

            // or delete it
            r.Trash(bean);

            // close the connection
            r.Dispose();
        }

    }

}