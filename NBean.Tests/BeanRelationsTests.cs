using System;
using System.Collections.Generic;
using System.Linq;
using NBean.Exceptions;
using Xunit;
using Xunit.Sdk;

namespace NBean.Tests
{

    public class BeanRelationsTest : IDisposable
    {
        BeanApi _api;


        public BeanRelationsTest()
        {
            _api = SQLitePortability.CreateApi();
        }


        public void Dispose()
        {
            _api.Dispose();
        }


        public void CreateLinkTestScenario(bool withLinks = true)
        {
            _api.Exec("CREATE TABLE Store (id INTEGER NOT NULL PRIMARY KEY, Name)");
            _api.Exec("CREATE TABLE Product (id INTEGER NOT NULL PRIMARY KEY, Name)");
            _api.Exec("CREATE TABLE Supplier (id INTEGER NOT NULL PRIMARY KEY, Name)");
            _api.Exec("CREATE TABLE StoreProduct_link (id INTEGER NOT NULL PRIMARY KEY, Store_id, Product_id, OnStock, IsSale)");
            _api.Exec("CREATE TABLE SupplierProduct_link (id INTEGER NOT NULL PRIMARY KEY, Supplier_id, Product_id, DeliveryTime)");

            _api.Dispense("Store").Put("Name", "Main Store").Store();
            _api.Dispense("Store").Put("Name", "Oxford Street").Store();
            _api.Dispense("Store").Put("Name", "Madison Avenue").Store();

            _api.Dispense("Product").Put("Name", "MacBook Pro 13").Store();
            _api.Dispense("Product").Put("Name", "Microsoft Surface IV").Store();
            _api.Dispense("Product").Put("Name", "Lenovo ThinkPad X1").Store();
            _api.Dispense("Product").Put("Name", "Dell XPS 13").Store();
            _api.Dispense("Product").Put("Name", "Lenovo Yoga").Store();
            _api.Dispense("Supplier").Put("Name", "CheaperNotebooks").Store();
            _api.Dispense("Supplier").Put("Name", "Mike''s Notebooks").Store();
            _api.Dispense("Supplier").Put("Name", "Laptops Galore").Store();


            if (!withLinks)
                return;
        
            _api.Exec("INSERT INTO StoreProduct_link VALUES(1, 1, 1, 4, false)");
            _api.Exec("INSERT INTO StoreProduct_link VALUES(2, 1, 2, 5, false)");
            _api.Exec("INSERT INTO StoreProduct_link VALUES(3, 1, 3, 2, true)");
            _api.Exec("INSERT INTO StoreProduct_link VALUES(4, 1, 4, 9, false)");
            _api.Exec("INSERT INTO StoreProduct_link VALUES(5, 1, 5, 6, true)");
            _api.Exec("INSERT INTO StoreProduct_link VALUES(6, 2, 3, 10, false)");
            _api.Exec("INSERT INTO StoreProduct_link VALUES(7, 2, 5, 7, false)");
            _api.Exec("INSERT INTO StoreProduct_link VALUES(8, 3, 1, 15, false)");
            _api.Exec("INSERT INTO StoreProduct_link VALUES(9, 3, 2, 21, false)");

            _api.Exec("INSERT INTO SupplierProduct_link VALUES(1, 1, 3, 7)");
            _api.Exec("INSERT INTO SupplierProduct_link VALUES(2, 1, 5, 7)");
            _api.Exec("INSERT INTO SupplierProduct_link VALUES(3, 2, 1, 7)");
            _api.Exec("INSERT INTO SupplierProduct_link VALUES(4, 2, 2, 7)");
            _api.Exec("INSERT INTO SupplierProduct_link VALUES(5, 2, 4, 7)");
            _api.Exec("INSERT INTO SupplierProduct_link VALUES(6, 3, 1, 3)");
            _api.Exec("INSERT INTO SupplierProduct_link VALUES(7, 3, 2, 3)");
            _api.Exec("INSERT INTO SupplierProduct_link VALUES(8, 3, 3, 3)");
            _api.Exec("INSERT INTO SupplierProduct_link VALUES(9, 3, 4, 5)");
            _api.Exec("INSERT INTO SupplierProduct_link VALUES(0, 3, 5, 5)");
        }



        [Fact]
        public void GetsFkName()
        {
            var testBean = _api.Dispense("testBean");
            Assert.Equal("testBean_id", testBean.GetFkName(testBean.GetKind()));
        }


        [Fact]
        public void AttachesBean()
        {
            _api.Exec("CREATE TABLE Bean1 (id, Prop)");
            _api.Exec("INSERT INTO Bean1 VALUES(1, 'Bean1Prop')");
            _api.Exec("CREATE TABLE Bean4 (id, Bean1_id, Prop)");
            _api.Exec("INSERT INTO Bean4 VALUES (1, null, 'Bean4Prop1')");

            var bean1 = _api.Load("Bean1", 1);
            // existing Bean
            var bean41 = _api.Load("Bean4", 1);
            // new Bean
            var bean42 = _api.Dispense("Bean4").Put("Prop", "Bean4Prop2");

            Assert.True(bean1.AttachOwned(bean41));
            Assert.True(bean1.AttachOwned(bean42));

            var ownedBeans = bean1.GetOwnedList("Bean4");
            var cOwnedBeans = bean1.GetOwnedList<Bean4>();

            Assert.Equal(2, ownedBeans.Count);
            Assert.Equal(2, cOwnedBeans.Count);
            Assert.Equal("Bean4Prop1", ownedBeans[0]["Prop"]);
            Assert.Equal("Bean4Prop2", ownedBeans[1]["Prop"]);
            Assert.Equal("Bean4Prop1", cOwnedBeans[0]["Prop"]);
            Assert.Equal("Bean4Prop2", cOwnedBeans[1]["Prop"]);
        }


        [Fact]
        public void AttachesBeanWithAlias()
        {
            _api.Exec("CREATE TABLE Bean1 (id, Prop)");
            _api.Exec("INSERT INTO Bean1 VALUES(1, 'Bean1Prop')");
            _api.Exec("CREATE TABLE Bean4 (id, Bean1Alias_id, Prop)");
            _api.Exec("INSERT INTO Bean4 VALUES (1, null, 'Bean4Prop1')");

            var bean1 = _api.Load("Bean1", 1);
            // existing Bean
            var bean41 = _api.Load("Bean4", 1);
            // new Bean
            var bean42 = _api.Dispense("Bean4").Put("Prop", "Bean4Prop2");

            Assert.True(bean1.AttachOwned(bean41, "Bean1Alias"));
            Assert.True(bean1.AttachOwned(bean42, "Bean1Alias"));

            var ownedBeans = bean1.GetOwnedList("Bean4", "Bean1Alias");
            var cOwnedBeans = bean1.GetOwnedList<Bean4>("Bean1Alias");

            Assert.Equal(2, ownedBeans.Count);
            Assert.Equal(2, cOwnedBeans.Count);
            Assert.Equal("Bean4Prop1", ownedBeans[0]["Prop"]);
            Assert.Equal("Bean4Prop2", ownedBeans[1]["Prop"]);
            Assert.Equal("Bean4Prop1", cOwnedBeans[0]["Prop"]);
            Assert.Equal("Bean4Prop2", cOwnedBeans[1]["Prop"]);
        }


        [Fact]
        public void AttachesOwnedBeans()
        {
            _api.Exec("CREATE TABLE Bean1 (id, Prop)");
            _api.Exec("INSERT INTO Bean1 VALUES(1, 'Bean1Prop')");
            _api.Exec("CREATE TABLE Bean4 (id, Bean1_id, Prop)");
            _api.Exec("INSERT INTO Bean4 VALUES (1, null, 'Bean4Prop1')");
            _api.Exec("INSERT INTO Bean4 VALUES (2, null, 'Bean4Prop2')");

            var bean1 = _api.Load("Bean1", 1);
            var beanList = new List<Bean>()
            {
                // existing Beans
                _api.Load("Bean4", 1),
                _api.Load("Bean4", 2),
                // new Bean
                _api.Dispense("Bean4").Put("Prop", "Bean4Prop3")
            };

            Assert.True(bean1.AttachOwned(beanList));

            beanList = bean1.GetOwnedList("Bean4").ToList();

            Assert.Equal(1L, beanList[0]["Bean1_id"]);
            Assert.Equal("Bean4Prop1", beanList[0]["Prop"]);
            Assert.Equal(1L, beanList[1]["Bean1_id"]);
            Assert.Equal("Bean4Prop2", beanList[1]["Prop"]);
            Assert.Equal(1L, beanList[2]["Bean1_id"]);
            Assert.Equal("Bean4Prop3", beanList[2]["Prop"]);
        }


        [Fact]
        public void AttachesOwnedBeansWithAlias()
        {
            _api.Exec("CREATE TABLE Bean1 (id, Prop)");
            _api.Exec("INSERT INTO Bean1 VALUES(1, 'Bean1Prop')");
            _api.Exec("CREATE TABLE Bean4 (id, Bean1Alias_id, Prop)");
            _api.Exec("INSERT INTO Bean4 VALUES (1, null, 'Bean4Prop1')");
            _api.Exec("INSERT INTO Bean4 VALUES (2, null, 'Bean4Prop2')");

            var bean1 = _api.Load("Bean1", 1);
            var beanList = new List<Bean>()
            {
                // existing Beans
                _api.Load("Bean4", 1),
                _api.Load("Bean4", 2),
                // new Bean
                _api.Dispense("Bean4").Put("Prop", "Bean4Prop3")
            };

            Assert.True(bean1.AttachOwned(beanList, "Bean1Alias"));

            beanList = bean1.GetOwnedList("Bean4", "Bean1Alias").ToList();

            Assert.Equal(1L, beanList[0]["Bean1Alias_id"]);
            Assert.Equal("Bean4Prop1", beanList[0]["Prop"]);
            Assert.Equal(1L, beanList[1]["Bean1Alias_id"]);
            Assert.Equal("Bean4Prop2", beanList[1]["Prop"]);
            Assert.Equal(1L, beanList[2]["Bean1Alias_id"]);
            Assert.Equal("Bean4Prop3", beanList[2]["Prop"]);
        }


        [Fact]
        public void DetachesOwnedBean()
        {
            _api.Exec("CREATE TABLE Bean1 (id, Prop)");
            _api.Exec("INSERT INTO Bean1 VALUES(1, 'Bean1Prop')");
            _api.Exec("CREATE TABLE Bean4 (id, Bean1_id, Prop)");
            _api.Exec("INSERT INTO Bean4 VALUES (1, 1, 'Bean4Prop1')");
            _api.Exec("INSERT INTO Bean4 VALUES (2, 1, 'Bean4Prop2')");
            _api.Exec("INSERT INTO Bean4 VALUES (3, 1, 'Bean4Prop3')");

            var bean1 = _api.Load("Bean1", 1);
            var beanList = bean1.GetOwnedList("Bean4").ToList();

            // Delete related
            Assert.True(bean1.DetachOwned(beanList[1], true));

            // Detach related
            Assert.True(bean1.DetachOwned(beanList[2]));

            beanList = bean1.GetOwnedList("Bean4").ToList();

            Assert.Single(beanList);

            var orphanedBean = _api.Load("Bean4", 3);

            Assert.Equal("Bean4Prop1", beanList[0]["Prop"]);
            Assert.Null(orphanedBean["Bean1_id"]);
            Assert.Equal("Bean4Prop3", orphanedBean["Prop"]);
        }


        [Fact]
        public void DetachesOwnedBeanWithAlias()
        {
            _api.Exec("CREATE TABLE Bean1 (id, Prop)");
            _api.Exec("INSERT INTO Bean1 VALUES(1, 'Bean1Prop')");
            _api.Exec("CREATE TABLE Bean4 (id, Bean1Alias_id, Prop)");
            _api.Exec("INSERT INTO Bean4 VALUES (1, 1, 'Bean4Prop1')");
            _api.Exec("INSERT INTO Bean4 VALUES (2, 1, 'Bean4Prop2')");
            _api.Exec("INSERT INTO Bean4 VALUES (3, 1, 'Bean4Prop3')");

            var bean1 = _api.Load("Bean1", 1);
            var beanList = bean1.GetOwnedList("Bean4", "Bean1Alias").ToList();

            // Delete related
            Assert.True(bean1.DetachOwned(beanList[1], true, "Bean1Alias"));

            // Detach related
            Assert.True(bean1.DetachOwned(beanList[2], false, "Bean1Alias"));

            beanList = bean1.GetOwnedList("Bean4", "Bean1Alias").ToList();

            Assert.Single(beanList);

            var orphanedBean = _api.Load("Bean4", 3);

            Assert.Equal("Bean4Prop1", beanList[0]["Prop"]);
            Assert.Null(orphanedBean["Bean1Alias_id"]);
            Assert.Equal("Bean4Prop3", orphanedBean["Prop"]);
        }


        [Fact]
        public void DetachesOwnedBeans()
        {
            _api.Exec("CREATE TABLE Bean1 (id, Prop)");
            _api.Exec("INSERT INTO Bean1 VALUES(1, 'Bean1Prop')");
            _api.Exec("CREATE TABLE Bean4 (id, Bean1_id, Prop)");
            _api.Exec("INSERT INTO Bean4 VALUES (1, 1, 'Bean4Prop1')");
            _api.Exec("INSERT INTO Bean4 VALUES (2, 1, 'Bean4Prop2')");
            _api.Exec("INSERT INTO Bean4 VALUES (3, 1, 'Bean4Prop3')");
            _api.Exec("INSERT INTO Bean4 VALUES (4, 1, 'Bean4Prop4')");

            var bean1 = _api.Load("Bean1", 1);
            var beanList = bean1.GetOwnedList("Bean4").ToList();
            var detachDelBeans = beanList.Where(b => b.Get<Int64>("id") % 2 == 0).ToList();

            // Delete related
            Assert.True(bean1.DetachOwned(detachDelBeans, true));

            beanList = bean1.GetOwnedList("Bean4").ToList();
            Assert.Equal(2, beanList.Count);
            Assert.Equal(1L, beanList[0]["id"]);
            Assert.Equal(3L, beanList[1]["id"]);

            // Detach related
            Assert.True(bean1.DetachOwned(beanList));

            beanList = bean1.GetOwnedList("Bean4").ToList();

            Assert.Empty(beanList);

            var orphanedBeanList = _api.Find("Bean4", "WHERE Bean1_id IS NULL");

            Assert.Equal(2, orphanedBeanList.Length);
            Assert.Equal("Bean4Prop1", orphanedBeanList[0]["Prop"]);
            Assert.Equal("Bean4Prop3", orphanedBeanList[1]["Prop"]);
        }


        [Fact]
        public void DetachesOwnedBeansWithAlias()
        {
            _api.Exec("CREATE TABLE Bean1 (id, Prop)");
            _api.Exec("INSERT INTO Bean1 VALUES(1, 'Bean1Prop')");
            _api.Exec("CREATE TABLE Bean4 (id, Bean1Alias_id, Prop)");
            _api.Exec("INSERT INTO Bean4 VALUES (1, 1, 'Bean4Prop1')");
            _api.Exec("INSERT INTO Bean4 VALUES (2, 1, 'Bean4Prop2')");
            _api.Exec("INSERT INTO Bean4 VALUES (3, 1, 'Bean4Prop3')");
            _api.Exec("INSERT INTO Bean4 VALUES (4, 1, 'Bean4Prop4')");

            var bean1 = _api.Load("Bean1", 1);
            var beanList = bean1.GetOwnedList("Bean4", "Bean1Alias").ToList();
            var detachDelBeans = beanList.Where(b => b.Get<Int64>("id") % 2 == 0).ToList();

            // Delete related
            Assert.True(bean1.DetachOwned(detachDelBeans, true));

            beanList = bean1.GetOwnedList("Bean4", "Bean1Alias").ToList();
            Assert.Equal(2, beanList.Count);
            Assert.Equal(1L, beanList[0]["id"]);
            Assert.Equal(3L, beanList[1]["id"]);

            // Detach related
            Assert.True(bean1.DetachOwned(beanList, false, "Bean1Alias"));

            beanList = bean1.GetOwnedList("Bean4", "Bean1Alias").ToList();

            Assert.Empty(beanList);

            var orphanedBeanList = _api.Find("Bean4", "WHERE Bean1Alias_id IS NULL");

            Assert.Equal(2, orphanedBeanList.Length);
            Assert.Equal("Bean4Prop1", orphanedBeanList[0]["Prop"]);
            Assert.Equal("Bean4Prop3", orphanedBeanList[1]["Prop"]);
        }


        [Fact]
        public void GetsOwner()
        {
            _api.Exec("CREATE TABLE Bean1 (id, Prop)");
            _api.Exec("INSERT INTO Bean1 VALUES(1, 'Bean1Prop')");
            _api.Exec("CREATE TABLE Bean4 (id, Bean1_id, Prop)");
            _api.Exec("INSERT INTO Bean4 VALUES (1, 1, 'Bean4Prop1')");
            _api.Exec("INSERT INTO Bean4 VALUES (2, null, 'Bean4Prop2')");

            var bean1 = _api.Load("Bean1", 1);
            var bean41 = _api.Load("Bean4", 1);
            var bean42 = _api.Load("Bean4", 2);

            Assert.Equal(bean1.Data, bean41.GetOwner("Bean1").Data);
            Assert.Equal(bean1.Data, bean41.GetOwner<Bean1>().Data);
            Assert.Null(bean42.GetOwner("Bean1"));
            Assert.Null(bean42.GetOwner<Bean1>());
        }


        [Fact]
        public void GetsOwnerWithAlias()
        {
            _api.Exec("CREATE TABLE Bean1 (id, Prop)");
            _api.Exec("INSERT INTO Bean1 VALUES(1, 'Bean1Prop')");
            _api.Exec("CREATE TABLE Bean4 (id, Bean1Alias_id, Prop)");
            _api.Exec("INSERT INTO Bean4 VALUES (1, 1, 'Bean4Prop1')");
            _api.Exec("INSERT INTO Bean4 VALUES (2, null, 'Bean4Prop2')");

            var bean1 = _api.Load("Bean1", 1);
            var bean41 = _api.Load("Bean4", 1);
            var bean42 = _api.Load("Bean4", 2);

            Assert.Equal(bean1.Data, bean41.GetOwner("Bean1", "Bean1Alias").Data);
            Assert.Equal(bean1.Data, bean41.GetOwner<Bean1>("Bean1Alias").Data);
            Assert.Null(bean42.GetOwner("Bean1", "Bean1Alias"));
            Assert.Null(bean42.GetOwner<Bean1>("Bean1Alias"));
        }


        [Fact]
        public void AttachesOwner()
        {
            _api.Exec("CREATE TABLE Bean1 (id, Prop)");
            _api.Exec("INSERT INTO Bean1 VALUES(1, 'Bean1Prop1')");
            _api.Exec("INSERT INTO Bean1 VALUES(2, 'Bean1Prop2')");
            _api.Exec("CREATE TABLE Bean3 (id, Prop)");
            _api.Exec("INSERT INTO Bean3 VALUES(1, 'Bean3Prop')");
            _api.Exec("CREATE TABLE Bean4 (id, Bean1_id, Prop)");
            _api.Exec("INSERT INTO Bean4 VALUES (1, 1, 'Bean4Prop1')");
            _api.Exec("INSERT INTO Bean4 VALUES (2, null, 'Bean4Prop2')");

            var bean11 = _api.Load("Bean1", 1);
            var bean12 = _api.Load("Bean1", 1);
            var bean31 = _api.Load("Bean3", 1);
            var bean41 = _api.Load("Bean4", 1);
            var bean42 = _api.Load("Bean4", 2);

            Assert.Null(bean42.GetOwner("Bean1"));

            Assert.Throws<MissingForeignKeyColumnException>(() => bean41.AttachOwner(bean31));

            Assert.True(bean41.AttachOwner(bean12));
            Assert.True(bean42.AttachOwner(bean11));

            Assert.Equal(bean11.Data, bean41.GetOwner("Bean1").Data);
            Assert.Equal(bean12.Data, bean42.GetOwner("Bean1").Data);
        }


        [Fact]
        public void AttachesOwnerWithAlias()
        {
            _api.Exec("CREATE TABLE Bean1 (id, Prop)");
            _api.Exec("INSERT INTO Bean1 VALUES(1, 'Bean1Prop1')");
            _api.Exec("INSERT INTO Bean1 VALUES(2, 'Bean1Prop2')");
            _api.Exec("CREATE TABLE Bean3 (id, Prop)");
            _api.Exec("INSERT INTO Bean3 VALUES(1, 'Bean3Prop')");
            _api.Exec("CREATE TABLE Bean4 (id, Bean1Alias_id, Prop)");
            _api.Exec("INSERT INTO Bean4 VALUES (1, 1, 'Bean4Prop1')");
            _api.Exec("INSERT INTO Bean4 VALUES (2, null, 'Bean4Prop2')");

            var bean11 = _api.Load("Bean1", 1);
            var bean12 = _api.Load("Bean1", 1);
            var bean31 = _api.Load("Bean3", 1);
            var bean41 = _api.Load("Bean4", 1);
            var bean42 = _api.Load("Bean4", 2);

            Assert.Null(bean42.GetOwner("Bean1", "Bean1Alias"));

            Assert.Throws<MissingForeignKeyColumnException>(() => bean41.AttachOwner(bean31, "BeanxAlias"));

            Assert.True(bean41.AttachOwner(bean12, "Bean1Alias"));
            Assert.True(bean42.AttachOwner(bean11, "Bean1Alias"));

            Assert.Equal(bean11.Data, bean41.GetOwner("Bean1", "Bean1Alias").Data);
            Assert.Equal(bean12.Data, bean42.GetOwner("Bean1", "Bean1Alias").Data);
        }


        [Fact]
        public void DetachesOwner()
        {
            _api.Exec("CREATE TABLE Bean1 (id, Prop)");
            _api.Exec("INSERT INTO Bean1 VALUES(1, 'Bean1Prop1')");
            _api.Exec("CREATE TABLE Bean4 (id, Bean1_id, Prop)");
            _api.Exec("INSERT INTO Bean4 VALUES (1, 1, 'Bean4Prop1')");
            _api.Exec("INSERT INTO Bean4 VALUES (2, 1, 'Bean4Prop2')");

            var bean1 = _api.Load("Bean1", 1);
            var ownedList = bean1.GetOwnedList("Bean4");

            Assert.True(ownedList[0].DetachOwner(bean1.GetKind()));
            Assert.True(ownedList[1].DetachOwner(bean1.GetKind(), true));

            ownedList = bean1.GetOwnedList("Bean4");
            var bean41 = _api.Load("Bean4", 1);

            Assert.Empty(ownedList);
            Assert.Null(bean41["Bean1_id"]);
        }


        [Fact]
        public void DetachesOwnerWithAlias()
        {
            _api.Exec("CREATE TABLE Bean1 (id, Prop)");
            _api.Exec("INSERT INTO Bean1 VALUES(1, 'Bean1Prop1')");
            _api.Exec("CREATE TABLE Bean4 (id, Bean1Alias_id, Prop)");
            _api.Exec("INSERT INTO Bean4 VALUES (1, 1, 'Bean4Prop1')");
            _api.Exec("INSERT INTO Bean4 VALUES (2, 1, 'Bean4Prop2')");

            var bean1 = _api.Load("Bean1", 1);
            var ownedList = bean1.GetOwnedList("Bean4", "Bean1Alias");

            Assert.True(ownedList[0].DetachOwner(bean1.GetKind(), false, "Bean1Alias"));
            Assert.True(ownedList[1].DetachOwner(bean1.GetKind(), true, "Bean1Alias"));

            ownedList = bean1.GetOwnedList("Bean4", "Bean1Alias");
            var bean41 = _api.Load("Bean4", 1);

            Assert.Empty(ownedList);
            Assert.Null(bean41["Bean1Alias_id"]);
        }


        [Fact]
        public void DetachesCustomOwner()
        {
            _api.Exec("CREATE TABLE Bean1 (id, Prop)");
            _api.Exec("INSERT INTO Bean1 VALUES(1, 'Bean1Prop1')");
            _api.Exec("CREATE TABLE Bean4 (id, Bean1_id, Prop)");
            _api.Exec("INSERT INTO Bean4 VALUES (1, 1, 'Bean4Prop1')");
            _api.Exec("INSERT INTO Bean4 VALUES (2, 1, 'Bean4Prop2')");

            var bean1 = _api.Load<Bean1>(1);
            var ownedList = bean1.GetOwnedList<Bean4>();

            Assert.True(ownedList[0].DetachOwner<Bean1>());
            Assert.True(ownedList[1].DetachOwner<Bean1>(true));

            ownedList = bean1.GetOwnedList<Bean4>();
            var bean41 = _api.Load("Bean4", 1);

            Assert.Empty(ownedList);
            Assert.Null(bean41["Bean1_id"]);
        }


        [Fact]
        public void DetachesCustomOwnerWithAlias()
        {
            _api.Exec("CREATE TABLE Bean1 (id, Prop)");
            _api.Exec("INSERT INTO Bean1 VALUES(1, 'Bean1Prop1')");
            _api.Exec("CREATE TABLE Bean4 (id, Bean1Alias_id, Prop)");
            _api.Exec("INSERT INTO Bean4 VALUES (1, 1, 'Bean4Prop1')");
            _api.Exec("INSERT INTO Bean4 VALUES (2, 1, 'Bean4Prop2')");

            var bean1 = _api.Load<Bean1>(1);
            var ownedList = bean1.GetOwnedList<Bean4>("Bean1Alias");

            Assert.True(ownedList[0].DetachOwner<Bean1>(false, "Bean1Alias"));
            Assert.True(ownedList[1].DetachOwner<Bean1>(true, "Bean1Alias"));

            ownedList = bean1.GetOwnedList<Bean4>("Bean1Alias");
            var bean41 = _api.Load("Bean4", 1);

            Assert.Empty(ownedList);
            Assert.Null(bean41["Bean1Alias_id"]);
        }


        [Fact]
        public void GetsLinkScenario()
        {
            _api.Exec("CREATE TABLE Store (id, Name)");
            _api.Exec("CREATE TABLE Product (id, Name)");
            _api.Exec("CREATE TABLE StoreProduct_link (id, Store_id, Product_id, OnStock, IsSale)");
            _api.Exec("INSERT INTO Store VALUES (1, 'Main Store')");

            var store = _api.Load("Store", 1);

            var ls = store.GetLinkScenario("Product");

            Assert.Equal("Store", ls.LinkingKind);
            Assert.Equal(store["id"], ls.LinkingKindPkValue);
            Assert.Equal("Store_id", ls.LinkingKindFkName);

            Assert.Equal("Product", ls.LinkedKind);
            Assert.Equal("Product_id", ls.LinkedKindFkName);
            Assert.Equal("id", ls.LinkedKindPkName);

            Assert.Equal("StoreProduct_link", ls.LinkKind);
            Assert.Equal("id", ls.LinkKindPkName);
        }


        [Fact]
        public void GetLinkedList()
        {
            CreateLinkTestScenario();

            var store = _api.Load("Store", 1);
            var storeProducts = store.GetLinkedList("Product");
            Assert.Equal(5, storeProducts.Count);

            var cStore = _api.Load<Store>(1);
            var cStoreProducts = cStore.GetLinkedList<Product>();
            Assert.Equal(5, cStoreProducts.Count);

            var product = _api.Load("Product", 2);
            var productStores = product.GetLinkedList("Store");
            Assert.Equal(2, productStores.Count);

            var cProduct = _api.Load<Product>(2);
            var cProductStores = cProduct.GetLinkedList<Store>();
            Assert.Equal(2, cProductStores.Count);

            var supplier = _api.Load("Supplier", 2);
            var supplierProducts = supplier.GetLinkedList("Product");
            Assert.Equal(3, supplierProducts.Count);

            var cSupplier = _api.Load<Supplier>(2);
            var cSupplierProducts = cSupplier.GetLinkedList<Product>();
            Assert.Equal(3, supplierProducts.Count);

            product = _api.Load("Product", 5);
            var productSuppliers = product.GetLinkedList("Supplier");
            Assert.Equal(2, productSuppliers.Count);

            cProduct = _api.Load<Product>(5);
            var cProductSuppliers = cProduct.GetLinkedList<Supplier>();
            Assert.Equal(2, cProductSuppliers.Count);

            var storeProductsExt = store.GetLinkedListEx("Product");
            Assert.Equal(5, storeProductsExt.Count);

            var cStoreProductsExt = cStore.GetLinkedListEx<Product>();
            Assert.Equal(5, cStoreProductsExt.Count);

            product = _api.Load("Product", 2);
            var productStoresExt = product.GetLinkedListEx("Store");
            Assert.Equal(2, productStoresExt.Count);

            cProduct = _api.Load<Product>(2);
            var cProductStoresExt = cProduct.GetLinkedListEx<Store>();
            Assert.Equal(2, cProductStoresExt.Count);

            var supplierProductsExt = supplier.GetLinkedListEx("Product");
            Assert.Equal(3, supplierProductsExt.Count);

            var cSupplierProductsExt = cSupplier.GetLinkedListEx<Product>();
            Assert.Equal(3, cSupplierProductsExt.Count);

            product = _api.Load("Product", 5);
            var productSuppliersExt = product.GetLinkedListEx("Supplier");
            Assert.Equal(2, productSuppliersExt.Count);

            cProduct = _api.Load<Product>(5);
            var cProductSuppliersExt = cProduct.GetLinkedListEx<Supplier>();
            Assert.Equal(2, cProductSuppliersExt.Count);
        }


        [Fact]
        public void CreatesLinks()
        {
            CreateLinkTestScenario(false);

            // single linking Store -> Product
            var mainStore = _api.Load("Store", 1);
            mainStore.LinkWith(_api.Load("Product", 1),
                new Dictionary<string, object>() { { "OnStock", 4 }, { "IsSale", false } });
            Assert.Single(mainStore.GetLinkedList("Product"));

            // single linking Product -> Store
            var surface = _api.Load("Product", 2);
            surface.LinkWith(mainStore,
                new Dictionary<string, object>() { { "OnStock", 5 }, { "IsSale", false } });
            Assert.Single(surface.GetLinkedList("Store"));
            Assert.Equal(2, mainStore.GetLinkedList("Product").Count);

            // multiple linking Store -> List<Product> w/o Link Props
            var productList = _api.Find(true, "Product", "WHERE id > 2").ToList();
            mainStore.LinkWith(productList);
            Assert.Equal(5, mainStore.GetLinkedList("Product").Count);

            // multiple linking Product -> List<Supplier> w Link Props
            var supplierList = _api.Find(true, "Supplier", "").ToList();
            surface.LinkWith(supplierList,
                new Dictionary<string, object>() { { "DeliveryTime", 3 } });
            Assert.Equal(3, surface.GetLinkedListEx("Supplier").Count);
        }


        [Fact]
        public void UnlinksBeans()
        {
            CreateLinkTestScenario();

            var mainStore = _api.Load("Store", 1);
            mainStore.Unlink(_api.Load("Product", 1));
            mainStore.Unlink(_api.Load("Product", 5));
            var storeProducts = mainStore.GetLinkedList("Product");
            Assert.Equal(3L, storeProducts.Count);

            mainStore.Unlink(new List<Bean>()
                {
                    _api.Load("Product", 2),
                    _api.Load("Product", 4)
                }
            );
            storeProducts = mainStore.GetLinkedList("Product");
            Assert.Single(storeProducts);
            Assert.Equal(3L, storeProducts[0]["id"]);
        }

    }


    partial class Bean1 : Bean
    {
        public Bean1() : base("Bean1")
        {
        }
    }


    partial class Bean4 : Bean
    {
        public Bean4() : base("Bean4")
        {
        }
    }


    partial class Store : Bean
    {
        public Store() : base("Store")
        {
        }
    }


    partial class Product : Bean
    {
        public Product() : base("Product")
        {
        }
    }


    partial class Supplier : Bean
    {
        public Supplier() : base("Supplier")
        {
        }
    }

}
