using System;
using System.Data.SQLite;
using NBean;

namespace LimeBean.Tests.Examples
{

    public class Northwind
    {

        class Category : Bean
        {

            public Category()
                : base("Category")
            {
            }

            // Typed property accessors

            public int? CategoryID
            {
                get => Get<int?>("CategoryID");
                set => Put("CategoryID", value);
            }

            public string CategoryName
            {
                get => Get<string>("CategoryName");
                set => Put("CategoryName", value);
            }

            public string Description
            {
                get => Get<string>("Description");
                set => Put("Description", value);
            }

            bool HasName => !string.IsNullOrWhiteSpace(CategoryName);

            // Helper method to find all products in this category
            // NOTE internal LRU cache is used, so DB is not hit every time
            public Product[] GetProducts()
            {
                return GetApi().Find<Product>("where CategoryID = {0}", CategoryID);
            }

            // Validation rules prevent storing of unnamed categories
            protected internal override void BeforeStore()
            {
                if (!HasName)
                    throw new Exception("Category name cannot be empty");
            }

            // Cascading delete of all its products
            // NOTE deletion is wrapped in an implicit transaction
            // so that consistency is maintained
            protected internal override void BeforeTrash()
            {
                foreach (var p in GetProducts())
                    GetApi().Trash(p);
            }

            public override string ToString()
            {
                return HasName 
                    ? CategoryName 
                    : base.ToString();
            }
        }

        class Product : Bean
        {

            public Product()
                : base("Product")
            {
            }

            // Typed accessors

            public int? ProductId
            {
                get => Get<int?>("ProductId");
                set => Put("ProductId", value);
            }

            public string Name
            {
                get => Get<string>("Name");
                set => Put("Name", value);
            }

            public int? CategoryID
            {
                get => Get<int?>("CategoryID");
                set => Put("CategoryID", value);
            }

            public decimal UnitPrice
            {
                get => Get<decimal>("UnitPrice");
                set => Put("UnitPrice", value);
            }

            public bool Discontinued
            {
                get => Get<bool>("Discontinued");
                set => Put("Discontinued", value);
            }

            // Example of a referenced bean
            // NOTE internal LRU cache is used during loading
            public Category Category
            {
                get => GetApi().Load<Category>(CategoryID);
                set => CategoryID = value.CategoryID;
            }

            // A set of validation checks
            protected internal override void BeforeStore()
            {
                if (string.IsNullOrWhiteSpace(Name))
                    throw new Exception("Product name cannot be empty");

                if (UnitPrice <= 0)
                    throw new Exception("Price must be a non-negative number");

                if (Category == null)
                    throw new Exception("Product must belong to an existing category");
            }
        }


        public void Scenario()
        {
            using (var r = new BeanApi("data source=:memory:", SQLiteFactory.Instance))
            {
                r.Key<Category>("CategoryID");
                r.Key<Product>("ProductID");
                r.EnterFluidMode();

                var beverages = r.Dispense<Category>();
                beverages.CategoryName = "Beverages";
                beverages.Description = "Soft drinks, coffees, teas, beers, and ales";

                var condiments = r.Dispense<Category>();
                condiments.CategoryName = "Condiments";
                condiments.Description = "Sweet and savory sauces, relishes, spreads, and seasonings";

                r.Store(beverages);
                r.Store(condiments);


                var chai = r.Dispense<Product>();
                chai.Name = "Chai";
                chai.UnitPrice = 18;
                chai.Category = beverages;

                var chang = r.Dispense<Product>();
                chang.Name = "Chang";
                chang.UnitPrice = 19;
                chang.Category = beverages;

                var syrup = r.Dispense<Product>();
                syrup.Name = "Aniseed Syrup";
                syrup.UnitPrice = 9.95M;
                syrup.Category = condiments;
                syrup.Discontinued = true;

                r.Store(chai);
                r.Store(chang);
                r.Store(syrup);

                Console.WriteLine($"Number of known beverages: {beverages.GetProducts().Length}");

                Console.WriteLine("Deleting all the beverages...");
                r.Trash(beverages);

                Console.WriteLine($"Products remained: {r.Count<Product>()}");
            }
        }

    }

}
