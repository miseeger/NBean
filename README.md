![LimeBeanLogo](C:/GIT/miseeger.NBean/Assets/NBeanLogo_md.png)



NBean is a fork of [LimeBean](https://github.com/Nick-Lucas/LimeBean) with a couple of addional features, exclusively targeting NetStandard 2.0. It is a [RedBeanPHP](http://redbeanphp.com/)-inspired ORM for .NET which provides a simple and concise API for accessing **ADO.NET** data sources. It's a **Hybrid-ORM** ... halfway between a micro-ORM and plain old SQL.

Supported databases include:

- **SQLite**
- **MySQL/MariaDB**
- **PostgreSQL**
- **SQL Server**



## Installation

NBean is not yet available on Nuget but a Nuget package is created on compilation.



## Getting started: Connecting

NBean needs an ADO.NET driver to work with. You can use one of the following:

- [System.Data.SQLite.Core](https://www.nuget.org/packages/System.Data.SQLite.Core) for SQLite in .NET
- [Microsoft.Data.SQLite](https://www.nuget.org/packages/Microsoft.Data.SQLite) for SQLite in .NET Core
- [System.Data.SqlClient](https://msdn.microsoft.com/en-us/library/System.Data.SqlClient.aspx) for SQL Server
- [MySql.Data](https://www.nuget.org/packages/MySql.Data/) official connector for MySQL or MariaDB
- [Npgsql](https://www.nuget.org/packages/Npgsql/) for PostgreSQL



To start using NBean, create an instance of the `BeanApi` class:

```csharp
// Using a connection string and an ADO.NET provider factory                
var api = new BeanApi("server=localhost; database=db1; ...", MySqlClientFactory.Instance);


// Using a connection string and a connection type
var api = new BeanApi("data source=/path/to/db", typeof(SQLiteConnection));


// Using a shared pre-opened connection
var api = new BeanApi(connection);
```

**NOTE:** `BeanApi` implements `IDisposable`. When created from a connection string (two first cases above), the underlying connection is initiated on the first usage and closed on dispose. Shared connections are used as-is, their state is not changed.

See also: [BeanApi Object Lifetime](#beanapi-object-lifetime)



## Getting Started: Basic CRUD (Create/Read/Update/Delete)

For basic usage, NBean requires no configuration or table classes!

Take a look at some basic CRUD scenarios:

**Create**

```csharp
// Create a Bean. 
// "Bean" means row, and "Dispense" makes an empty Bean for a table.
var bean = api.Dispense("book");

// Each bean has a "Kind". Kind is a synonym for "table name"
// You give a Bean its Kind when you Dispense it, or query the database
var kind = bean.GetKind();
Console.WriteLine(kind);

// Fill the new Bean with some data
bean["title"] = "Three Comrades";
bean["rating"] = 10;

// You can also chain .Put() to do this
bean.Put("title", "Three Comrades")
    .Put("rating", 10);

// Store it
// Store() will Create or Update a record intelligently
var id = api.Store(bean);

// Store also returns the Primary Key for the saved Bean, even for multi-column/compound keys
Console.WriteLine(id);
```

**Read** and **Update**

```cs
// Load a Bean with a known ID
bean = api.Load("book", id);

// Make some edits
bean["release_date"] = new DateTime(2015, 7, 30);
bean["rating"] = 5;

// Update database
api.Store(bean);
```

**Delete**

```cs
api.Trash(bean);
```



## Typed Accessors

To access bean properties in a strongly-typed fashion, use the `Get<T>` method:

```cs
bean.Get<string>("title");
bean.Get<decimal>("price");
bean.Get<bool?>("someFlag");
```

And there is a companion `Put` method which is chainable:

```cs
bean
    .Put("name", "Jane Doe")
    .Put("comment", null);
```

See also: [Custom Bean Classes](#custom-bean-classes)



## Bean Options

You can configure the BeanAPI to dispense new Beans with some default options

**ValidateGetColumns**

```cs
// Sets whether a Bean throws `ColumnNotFoundException` if 
// you request a column which isn't stored in the Bean. True by default
api.BeanOptions.ValidateGetColumns = true;

Bean bean = api.Dispense("books");
bean.Put("ColumnOne", 1); // Add a single column
int one = bean.Get<int>("ColumnOne"); // OK
int two = bean.Get<int>("ColumnTwo"); // throws ColumnNotFoundException
```



## Fluid Mode

NBean mitigates the common inconvenience associated with relational databases, namely necessity to manually create tables, columns and adjust their data types. In this sense, NBean takes SQL databases a little closer to NoSQL ones like MongoDB.

**Fluid Mode** is optional, turned off by default, and is recommended for use only during early development stages (particularly for prototyping and scaffolding). To enable it, invoke the `EnterFluidMode` method on the `BeanApi` object:

```cs
api.EnterFluidMode();

// Make a Bean for a table which doesn't yet exist
var bean = api.Dispense("book_types");

// Fill it with some data
// NBean will automatically detect Types and create columns with the correct Type
bean.Put("name", "War")
    .Put("fiction", true);

// Store will automatically create any missing tables (with an auto-incrementing 'id' column) and columns, 
// then add the Bean as a new row
var id = api.Store(bean);

// The bean is now available in the database
var savedBean = api.Load("book_types", id);
```

How does this work? When you save a Bean while in Fluid Mode, NBean analyzes its fields and compares their names and types to the database schema. If new data cannot be stored to an existing table, schema alteration occurs. NBean can create new tables, add missing columns, and widen data types. It will never truncate data or delete unused columns.

**NOTE:** NBean will not detect renamings.

**CAUTION:** Automatically generated schema is usually sub-optimal and lacks indexes which are essential for performance. When most planned tables are already in place, it is recommended you turn Fluid Mode off, audit the database structure, add indexes, and make further schema changes with a dedicated database management tool (like HeidiSQL, SSMS, pgAdmin, etc).



## Finding Beans with SQL

NBean doesn't introduce any custom query language, nor does it implement a LINQ provider. To find beans matching a criteria, use fragments of plain SQL:

```cs
var list = api.Find("book", "WHERE rating > 7");
```

Instead of embedding values into SQL code, it is recommended to use **parameters**:

```cs
var list = api.Find("book", "WHERE rating > {0}", 7);
```

Usage of parameters looks similar to `String.Format`, but instead of direct interpolation, they are transformed into fair ADO.NET command parameters to protect your queries from SQL-injection attacks.

```cs
var list = api.Find(
    "book", 
    "WHERE release_date BETWEEN {0} and {1} AND author LIKE {2}",
    new DateTime(1930, 1, 1), new DateTime(1950, 1, 1), "%remarque%"
);
```

You can use any SQL as long as the result maps to a set of beans. For other cases, see [Generic Queries](#generic-sql-queries).

To find a single bean:

```cs
var best = api.FindOne("book", "ORDER BY rating DESC LIMIT 1");
```

To find out the number of beans without loading them:

```cs
var count = api.Count("book", "WHERE rating > {0}", 7);
```

It is also possible to perform unbuffered (memory-optimized) load for processing in a `foreach` loop.

Data is 'Lazy Loaded' on each iteration using [C-sharp's IEnumerable Yield](http://programmers.stackexchange.com/a/97350)

```cs
foreach (var bean in api.FindIterator("book", "ORDER BY rating")) {
    // do something with bean
}
```



## Custom Bean Classes

You can create Table classes like in a full ORM: It's convenient to inherit from the base `Bean` class:

```cs
public class Book : Bean {
    public Book()
        : base("book") {
    }

    public string Title {
        get { return Get<string>("title"); }
        set { Put("title", value); }
    }

    // ...
}
```

Doing so has several advantages:

- All strings prone to typos (bean kind and field names) are encapsulated inside.
- You get compile-time checks, IDE assistance and [typed properties](#typed-accessors).
- With [Lifecycle Hooks](#lifecycle-hooks), it is easy to implement [data validation](#data-validation) and [relations](#relations).

For [Custom Beans Classes](#custom-bean-classes), use method overloads with a generic parameter:

```cs
api.Dispense<Book>();
api.Load<Book>(1);
api.Find<Book>("WHERE rating > {0}", 7);
// and so on
```

### Using `nameof()`

With the help of the [nameof](https://msdn.microsoft.com/en-us/library/dn986596.aspx) operator (introduced in C# 6 / Visual Studio 2015), it's possible to define properties without using strings at all:

```cs
public string Title {
    get { return Get<string>(nameof(Title)); }
    set { Put(nameof(Title), value); }
}
```



## Lifecycle Hooks

[Custom Bean Classes](#custom-bean-classes) provide lifecycle hook methods which you can override to receive notifications about [CRUD operations](#getting-started-basic-crud-create-read-update-delete) occurring to this bean:

```cs
public class Product : Bean {
    public Product()
        : base("product") {
    }

    protected override void AfterDispense() { }
    
    protected override void BeforeLoad() { }
    
    protected override void AfterLoad() { }

    protected override void BeforeStore() { }
    protected override void BeforeInsert() { }
    protected override void BeforeUpdate() { }

    protected override void AfterStore() { }
    protected override void AfterInsert() { }
    protected override void AfterUpdate() { }

    protected override void BeforeTrash() { }

    protected override void AfterTrash() { }
}
```

Particularly useful are `BeforeStore` and `BeforeTrash` methods. They can be used for [validation](#data-validation), implementing [relations](#relations), assigning default values, etc.

See also: [Bean Observers](#bean-observers)



## Primary Keys

By default, all beans have auto-incrementing integer key named `"id"`. Keys are customizable in all aspects:

```cs
// Custom key name for beans of kind "book"
api.Key("book", "book_id");

// Custom key name for custom bean class Book (see Custom Bean Classes)
api.Key<Book>("book_id");

// Custom non-autoincrement key
api.Key("book", "book_id", false);

// Compound key (order_id, product_id) for beans of kind "order_item"
api.Key("order_item", "order_id", "product_id");

// Change defaults for all beans
api.DefaultKey("Oid", false);
```

**NOTE:** non auto-increment keys must be assigned manually prior to saving.

The [Bean Observers](#bean-observers) section contains an example of using GUID keys for all beans.



## Generic SQL Queries

Often it's needed to execute queries which don't map to beans: aggregates, grouping, joins, selecting single column, etc.

`BeanApi` provides methods for such tasks:

```cs
// Load multiple rows
var rows = api.Rows(@"SELECT author, COUNT(*) 
                      FROM book 
                      WHERE rating > {0} 
                      GROUP BY author", 7);

// Load a single row
var row = api.Row(@"SELECT author, COUNT(*) 
                    FROM book 
                    WHERE rating > {0}
                    GROUP BY author 
                    ORDER BY COUNT(*) DESC 
                    LIMIT 1", 7);

// Load a column
var col = api.Col<string>("SELECT DISTINCT author FROM book ORDER BY author");

// Load a single value
var count = api.Cell<int>("SELECT COUNT(*) FROM book");
```

For `Rows` and `Col`, there are unbuffered (memory-optimized) counterparts:

```cs
foreach(var row in api.RowsIterator("SELECT...")) {
    // do something
}

foreach(var item in api.ColIterator("SELECT...")) {
    // do something
}
```

To execute a non-query SQL command, use the `Exec` method:

```cs
api.Exec("SET autocommit = 0");
```

**NOTE:** all described functions accept parameters in the same form as [finder methods](#finding-beans-with-sql) do.



## Customizing SQL Commands

In some cases it is necessary to manually adjust parameters of a SQL command which is about to execute. This can be done in the `QueryExecuting` event handler.

**Example 1.**  Force `datetime2` type for all dates (SQL Server):

```cs
api.QueryExecuting += cmd => {
    foreach(SqlParameter p in cmd.Parameters)
        if(p.Value is DateTime)
            p.SqlDbType = SqlDbType.DateTime2;
};
```

**Example 2.** Work with `MySqlGeometry` objects (MySQL/MariaDB):

```cs
api.QueryExecuting += cmd => {
    foreach(MySqlParameter p in cmd.Parameters)
        if(p.Value is MySqlGeometry)
            p.MySqlDbType = MySqlDbType.Geometry;
};

bean["point"] = new MySqlGeometry(34.962, 34.066);
api.Store(bean);
```



## Data Validation

The `BeforeStore` [hook](#lifecycle-hooks) can be used to prevent bean from storing under certain circumstances. For example, let's define a [custom bean](#custom-bean-classes) `Book` which cannot be stored unless it has a non-empty title:

```cs
public class Book : Bean {
    public Book()
        : base("book") {
    }

    public string Title {
        get { return Get<string>("title"); }
        set { Put("title", value); }
    }

    protected override void BeforeStore() {
        if(String.IsNullOrWhiteSpace(Title))
            throw new Exception("Title must not be empty");
    }
}
```

See also: [Custom Bean Classes](#custom-bean-classes), [Lifecycle Hooks](#lifecycle-hooks)



## Custom Bean Class based Relations

Consider an example of two [custom beans](#custom-bean-classes): `Category` and `Product`:

```cs
public partial class Category : Bean {
    public Category()
        : base("category") {
    }

}

public partial class Product : Bean {
    public Product()
        : base("product") {
    }
}
```

We are going to link them so that a product knows its category, and a category can list all its products.

In the `Product` class, let's declare a method `GetCategory()`:

```cs
partial class Product {
    public Category GetCategory() {
        return GetApi().Load<Category>(this["category_id"]);
    }
}
```

In the `Category` class, we'll add a method named `GetProducts()`:

```cs
partial class Category {
    public Product[] GetProducts() {
        return GetApi().Find<Product>("WHERE category_id = {0}", this["id"]);
    }
}
```

> **NOTE:** NBean uses the [internal query cache](#internal-query-cache), therefore repeated `Load` and `Find` calls don't hit the database.

Now let's add some [validation logic](#data-validation) to prevent saving a product without a category and to prevent deletion of a non-empty category:

```cs
partial class Product {
    protected override void BeforeStore() {
        if(GetCategory() == null)
            throw new Exception("Product must belong to an existing category");
    }
}

partial class Category {
    protected override void BeforeTrash() {
        if(GetProducts().Any())
            throw new Exception("Category still contains products");
    }
}
```

Alternatively, we can implement cascading deletion:

``` cs
protected override void BeforeTrash() {
    foreach(var p in GetProducts())
        GetApi().Trash(p);
}
```

**NOTE:** `Store` and `Trash` always run in a transaction (see [Implicit Transactions](#implicit-transactions)), therefore even if something goes wrong inside the cascading deletion loop, database will remain in a consistent state!



## Convention based Relations

The second and convention based way to work with 1:n and m:n relations in NBeans comes out of the box. No need of any Custom Bean Classes but also Custom Beans are supported.

### 1:n Relations

#### Conventions for 1:n relations

The "n" part of this relational type is called "owned" (referencing) Bean. This Bean holds the Foreign Key that references the "1" part of this relational type, the "owner" (referenced) Bean. The name of the Foreign Key that is pointing to the Primary Key of the owner is built by the Kind (name) of the Bean suffixed an underscore (`_`) and by the name of the current Key name, e.g.: `Contact_id` or `Activity_id`. This foreign key mus be defined as column of the Bean's table in the database.

```sql
CREATE TABLE [Activity] (
    id INTEGER NOT NULL PRIMARY KEY,
    Description VARCHAR(64),
    ...
    Contact_id INTEGER  -- Foreign Key that points to the Contact owning the Activity
)
```

> It is absolutely necessary to respect the case sensitivity and correct spelling of the Bean Kind that corresponds to the first part of the Foreign Key Name. Misspelling or ignoring case sensitivity may end up in various errors.

#### Attaching to an owner 

An 1:n relation is established by "attaching" the owned Bean to the Owner. It is released by "detaching" the owner Bean (type).

```csharp
// get the owner Bean
var contact = _api.Load("Contact", 12);
            
// attach an existing Bean
var existingActivity = _api.Load("Activity", 123);
contact.AttachOwned(existingActivity);
// or
contact.AttachOwned(_api.Load("Activity", 123));
// or 
_api.Load("Contact", 12).AttachOwned(_api.Load("Activity", 123));

// attach a new Bean (is automatically stored when attaching)
// this can also be shortened in the way shown above
var newActivity = _api.Dispense("Activity").Put("Description", "Coffee break!");
contact.AttachOwned(newActivity);
```

Attaching owned Beans is not only limited to one Bean at a time. It is also possible to attach a list of Beans. When attaching Beans they may be either loaded or newly disposed.

```csharp
var contact = _api.Load("Contact", 12);
var activityList = new List<Bean>()
{
   	// existing Activities
    _api.Load("Activity", 4711),
    _api.Load("Activity", 121),
    // new Activities
    _api.Dispense("Activity").Put("Description", "Lunch break!")
};

contact.AttachOwned(activityList);
```

Attaching to an owner can also be made vice versa (from the "n" side of the relation). Maybe in order to change the Owner of an activity or relate an orphaned activity to its new owner.

```csharp
var activity = _api.Load("Activity", 123);
var owner = _api.Load("Contact", 1);

// attaching owner
activity.AttachOwner(owner);
```

#### Getting attached (owned) Beans

Getting a list of Beans that are attached to an owner is achieved by calling the `GetOwnedList()` method from an owner Bean. This method comes also with a version for Custom Beans: `GetOwnedList<T>()`.

```csharp
var contact = _api.Load("Contact", 12);
var contactActivitiesList = contact.GetOwnedList("Activity").ToList();

var customContact = _api.Load<Contact>(12);
var customContactActivitiesList = customContact.GetOwnedList<Activity>().ToList();
```

#### Getting the Owner of a Bean

It is also possible to get information about the related owner from the "n" side of the relation using the `GetOwner()` method or `GetOwner<T>()` method for Custom Beans. In this case it's only need to know the Kind or type of the Bean that is to get.

```csharp
var activity = _api.Load("Activity", 123);
var owningContact = _api.Load("Contact");

var activity2 = _api.Load("Acitivity" 42);
var customOnwingContact = _api.Load<Contact>();
```

#### Detaching from an Owner

The Beans that are attached to an owner may also be detached and by choice deleted or kept as more or less "orphaned" Bean.

```csharp
var contact = _api.Load("Contact", 1);
var myActivities = contact.GetOwnedList("Activity").ToList();

// detach first activity in list
contact.DetachOwned(myActivities.FirstOrDefault())       // <-- leaves the activity "orphaned"
contact.DetachOwned(myActivities.FirstOrDefault(), true) // <-- deletes the activity
    
// detach list of all "lunch" activities of the contact and leaves them "orphaned"
contact.DetachOwned(myActivities.Where(ma => ma.Get<string>("Description").Contains("Lunch")));

// detaches all "coffee" activities of the contact by deleting them
contact.DetachOwned(myActivities.Where(ma => ma.Get<string>("Description").Contains("Coffee")), true);
```

Detaching  from an owner can also be made vice versa (from the "n" side of the relation) and it also provides a version for Custom Beans. As with the other detach methods it's also possible to delete the owned Bean or leave it as "orphaned".

```csharp
var activity = _api.Load("Activity", 123);

activity.DetachOwner("Contact");       // -- leaves the activity "orphaned"
activity.DetachOwner("Contact", true); // -- deletes the activity

// using Custom Beans
activity.DetachOwner<Contact>();
activity.DetachOwner<Contact>(true);
```

### m:n Relations

m:n relations are implemented by using a link table in the background that stores the relations and establishes something like 1:n / n:1 under the hood. As we need this link table, we use the word "Link" for an m:n relation in NBeans. We are linking m Beans of Type X with n Beans of Type Y. For example Products that are sold in various stores or Products that are bought from different suppliers.  

#### Conventions for m:n relations

The link table will not be automatically generated from NBeans. It has to be created manually and adhere to a small number of conventions:

- The link table is named by putting together the names/kinds of the related beans, followed by `_link`. Be also aware of case sensitivity here! The names can be put in any order.
- A link table must have a Primary Key field (`id`,  by default)
- Either parts of the relation are represented by Foreign Key Fields, put together by the name/kind of the Bean and `_id` as suffix. Where `id` is the default key.
- Link tables may have further properties/columns in order to store link related information.
- Take in to consideration that link tables are also audited, if the `Auditor` Observer is in use. Blacklisting them is an option to omit them. 

The following definition of a link table relates Stores and Products

```sql
CREATE TABLE StoreProduct_link (
	id INTEGER NOT NULL PRIMARY KEY,
	Store_id INTEGER NOT NULL,
	Product_id INTEGER NOT NULL,
    OnStock INTEGER NOT NULL DEFAULT 0,
    IsSale BYTE NOT NULL DETAULT 0
)
```

Additional indexes may be necessary to improve the query performance.

#### Linking Beans

A link between two Beans is established from either part of the relation. It does not matter from which side to start. It is possible to link just one Bean or a list of Beans and also Custom Beans can be linked among each other or to basic Beans. If the link table (Link Bean) has columns (Properties) that must be provided with data then it's possible to pass the data in a `Dictionary<string, object>`. **Note:** When linking a list of Beans, the provided link data is saved with each linked Bean.

```csharp
var mainStore = _api.Load("Store", 1);

mainStore.LinkWith(_api.Load("Product", 1));

// providing link data
mainStore.LinkWith(_api.Load("Product", 4),
    new Dictionary<string, object>() { { "OnStock", 4 }, { "IsSale", false } } 
);

// linking all Products in "Main Store" also to an other store
var otherStore = _api.Load("Store", 3);

otherStore.LinkWith(_api.Find("Product", "WHERE Store_id = {0}", otherStore["id"]).ToList());

// providing same link data for each linked Bean 
otherStore.LinkWith(_api.Find("Product", "WHERE Store_id = {0}", otherStore["id"]).ToList(),
    new Dictionary<string, object>() { { "IsSale", false } } 
);
```

#### Getting linked Beans

Getting a list of linked Beans from an m:n relation is achieved by calling the `GetLinkedList()` method from either side of the relation. This method comes also with a version for Custom Beans: `GetLinkedList<T>()`. These methods only provide the linked Beans. In order to get the additional link data that is stored with each link, the `GetLinkedListEx()` or `GetLinkedListEx<T>()` method must be called. Both return a `Dictionary` which Key is the linked Bean and Value is the Link Bean containing the Foreigen Keys and link data.

```csharp
var store = _api.Load("Store", 1);
var storeProducts = store.GetLinkedList("Product");
var storeProductsExt = store.GetLinkedListEx("Product");

var customStore = _api.Load<Store>(1);
var customStoreProducts = cStore.GetLinkedList<Product>();
var customStoreProductsExt = cStore.GetLinkedListEx<Product>();
```

#### Unlinking Beans

Just like linking a Bean or a list of Beans it is possible to unlink a Bean or a list of Beans from a related Bean. Unlinking Beans can be done from either part of the relation. It does not matter which side to unlink. Also Custom Beans can be unlinked. Unlinking Beans means to delete the LinkBean that holds the Link (m:n relation).

```csharp
var mainStore = _api.Load("Store", 1);

// unlink one Product
mainStore.Unlink(_api.Load("Product", 1));

// unlink a list of Products
mainStore.Unlink(new List<Bean>()
    {
        _api.Load("Product", 2),
        _api.Load("Product", 4)
    }
);
```



## Transactions

To execute a block of code in a transaction, wrap it in a delegate and pass to the `Transaction` method:

```cs
api.Transaction(delegate() { 
    // do some work
});
```

Transaction is automatically rolled back if:

- An unhandled exception is thrown during the execution
- The delegate returns `false`

Otherwise it's committed.

Transactions can be nested (if the underlying ADO.NET provider allows this):

```cs
api.Transaction(delegate() {
    // outer transaction

    api.Transaction(delegate() { 
        // nested transaction
    });
});
```



## Implicit Transactions

When you invoke `Store` or `Trash` (see [CRUD]getting-started-basic-crud-create-read-update-delete) outside a transaction, then an implicit transaction is initiated behind the scenes. This is done to enforce database integrity in case of additional modifications performed in [hooks](#lifecycle-hooks) and [observers](#bean-observers) (such as cascading delete, etc).

There are special cases when you may need to turn this behavior off (for example when using [LOCK TABLES with InnoDB](https://dev.mysql.com/doc/refman/5.0/en/lock-tables-and-transactions.html)):

```cs
api.ImplicitTransactions = false;
```



## Bean Observers

Bean observers have the same purpose as [Lifecycle Hooks](#lifecycle-hooks) with the difference that former are invoked for all beans. With observers you can implement a kind of special plugins and extensions.

For example, let's make so that all beans have GUID keys instead of integer auto-increments:

```cs
class GuidKeyObserver : BeanObserver {
    public override void BeforeStore(Bean bean) {
        if(bean["id"] == null)
            bean["id"] = Guid.NewGuid();
    }
}

api.DefaultKey(false); // turn off auto-increment keys
api.AddObserver(new GuidKeyObserver());

// but beware of http://www.informit.com/articles/printerfriendly/25862
```

Another example is adding automatic timestamps:

```cs
class TimestampObserver : BeanObserver {
    public override void AfterDispense(Bean bean) {
        bean["created_at"] = DateTime.Now;
    }
    public override void BeforeStore(Bean bean) {
        bean["updated_at"] = DateTime.Now;
    }
}
```

As seen above, an Observer can be loaded into the API instance by using `AddObserver()`. To prevent repeated loading a set of Observers (or even only one Observer) again an again it is possible to add Observer instances to the static list of `InitialObservers`. While instancing the API those Observers will be added to the API, automatically:

```csharp
BeanApi.InitialObservers.AddRange(
    new List<BeanObserver>() {
    	new GuidKeyObserver(),
        new AuditorLight()
    }
);
```

> Note that the initially set Observers may not have the API itself in their constructor. Observers that need an API instance to be constructed have to be loaded by `AddObserver()`, shown in the first example.

### Further API methods to handle Observers

`GetObserver<T>()` - Gets the first loaded Observer instance of the given Type.

`IsObserverLoaded<T>()` - Checks if any Observer of the given Observer Type is loaded.

`HasObservers()` - Checks if any Observer is loaded.

`RemoveObserver<T>()` - Removes any loaded Observer of given Type.



## Auditing

Auditing means tracking of changes in special ways. Out of the box NBean comes with two kinds of Audit, implemented as Observer Plugins. The Auditor tracks any changes made with a Bean and logs them in a standard table called  `AUDIT` (capitalized!). Also new Beans and the deletion of beans are audited. The light version of auditing is provided by the Auditor Light Observer Plugin. It tracks creation and last change right into a bean if the needed properties (e. g. `CreatedAt` or `ChangedAt` ) are existing. Both Plugins can be used together but they work independently from each other.

### Current User

Auditing changes without the information who made the changes is pretty pointless. So the API provides a public Property `CurrentUser` that may be get and set. This information is taken to fill the user related fields of `Auditor` and `Auditor Light`. So be sure to set this Property a soon as the current user is known and immediately after the API is created. Independent from the audit functionality this `CurrentUser` can be used all over the system once the API is created.

### Auditor

The `Auditor` is an Observer Plugin that tracks changes made to Bean Properties (SQL UPDATE), creation of  Beans (SQL INSERT) and deletion of Beans (SQL DELETE). 

When the `Auditor` is instanciated in order to load it into the API it needs an API instance. As a second parameter a Blacklist of Bean Kinds (database tables) can be provided to prevent auditing changes made tho these Beans. By default `AUDIT` is blacklisted. The Beans are separated by semicolon (`;`). When instanciating the `Auditor` it checks if the `AUDIT` table is existing. If not, it is created by the Observer.

> Even if the `AUDIT` table is automatically created if not existing in the database it is highly recommended to create this table manually or by script because for creation the system will be put into [Fluid Mode](#fluid-mode).

```csharp
var api = new BeanApi(myConnection);

// registers the Auditor instance
api.AddObserver(new Auditor(api, string.Empty));
```

That's all you have to do to get (a quite verbose) auditing of changes in your database.

#### AUDIT Table

An `AUDIT` table for MS SQL Server can be created with the following script. For other databases, the types should be adapted accordingly. 

```SQL
CREATE TABLE [AUDIT] (
    id INTEGER NOT NULL PRIMARY KEY,
    Action VARCHAR(16),
    User VARCAHR(64),
    Object VARCHAR(64),
    ObjectId VARCHAR(64),
    Property VARCHAR(64),
    PropertyType VARCHAR(64),
    OldValue VARCHAR(1024),
    NewValue VARCHAR(1024),
    Notes VARCHAR(4096)
)
```

### Auditor Light

The `AuditorLight` provides a less verbose auditing and only needs at max four fields/properties in a bean/table to work:

```SQL
CreatedBy VARCHAR(64),
CreatedAt DATETIME,
ChangedBy VARCHAR(64),
ChangedAt DATETIME
```

The existence of these fields is checked on UPDATE or INSERT, filled with the current user and current date and saved with the changed or inserted data.

So just make sure the `AuditorLight` Observer is loaded and your tables have the needed columns and the Plugin  just works.

> **Note:** It is important that the column names are spelled correctly. Please note that the case-sensitivity.



## Slx Style Key Provider

Inspired by a CRM system called SalesLogix (now Infor CRM) there comes an Observer that provides auto incremental keys that theoretically generates 36^10^ (more than 3.65 quadrillion) unique Keys until there's an overflow. This number is reduced to about 2.65 quadrillion because of the starting Key, as you can read below.

An Slx style key is composed by a prefix which has five characters representing the table or bean name, either full if the original name has just five or less characters or reduced by the vowels. Names shorter than five characters are filled with `#`. so the following prefixes are generated by this logic:

| Bean name    | Prefix |
| ------------ | ------ |
| foo          | FOO##  |
| foobar       | FBR##  |
| foobarb      | FBRB#  |
| foobarbaz    | FBRBZ  |
| foobarbazetc | FBRBZ  |

This prefix is followed by a dash `-` and the auto incremented part of the key follows. It has a length of 10 and starts with `A000000000`. So the first 10 x 36^9^ Keys (about one quadrillion) can not be allocated. Like the Hex-System counts from 0 to F this system goes a couple of steps beyond. It counts from 0 to Z and that makes 36 values instead of 16. Examples for Slx Style keys are:

```
FOO##-A000000010
FOO##-A0000001A0
FOO##-B000000000
```

The Slx Style Key Provider is added to the Observers of an API instance and just works with the set default Primary Key Property (system default is `id`). The Constructor will receive the instance of the API. This is the reason why it can not be added to the initial Observers of an API. 

```csharp
var api = new BeanApi(myConnection);
api.AddObserver(new SlxKeyProvider(api));
```



## Plugins

Plugins privide the System with Functions and Actions (Delegates / Lambdas) that are registered in the API and invoked by their name, passing the needed parameters. As Actions they return nothing, just doing things. Functions will return a value.

### Types of Plugins

Ther are two types of Plugins. First there are Plugins that work only on API level. Those are the "normal" Actions and Functions.  Second there are Plugins (Actions and Functions) that work with Beans which includes the ability to access the API via `bean.Api`.

Usually Plugins are placed inside a static class from which they are registerd at the API. The following two sections gives an example of a "normal" Action and a Function and a Bean Action and Bean Function 

#### Writing API Plugins

In order to get the Plugins properly working, the signature is mandatory: `BeanApi` must be passed at first place, followed by `params object[]`.

```csharp
public static class PluginCollection
{
        
    public static void MyAction(BeanApi bApi, params object[] args) // <-- API must be delared as first parameter!
    {
        var output = (ITestOutputHelper)args[0];

        output.WriteLine($"Database Type: \"{bApi.DbType}\"");
        output.WriteLine($"Parameter: {args[1]}");
    }

    public static object MyFunction(BeanApi bApi, params object[] args) // <-- API must be declared as first parameter! 
    {
        return (int) args[0] * 2;
    }
}
```

#### Writing Bean Plugins

In order to get the Bean Plugins properly working, the signature is mandatory: `Bean`must be passed at first place, followed by `params object[]`.

```csharp
public static class PluginCollection
{

    public static void MyBeanAction(Bean aBean, params object[] args) // <-- Bean must be declared as first parameter!
    {
        Console.WriteLine($"The Bean is of kind: \"{aBean.GetKind()}\"");
        Console.WriteLine($"Parameter: {args[1]}");
    }

    public static object ReverseBeanKind(Bean aBean, params object[] args) // <-- Bean must be delared as first parameter!
    {
        var chArr = aBean.GetKind().ToCharArray();
        Array.Reverse((Array) chArr);
        
        return new string (chArr);
    }
}
```

### Register Plugins

To register "normal" Plugins use the `RegisterAction()` or `RegisterFunc()` method.

```csharp
var api = new BeanApi(myConnection);

// From a static class
api.RegisterAction("MyAction", PluginCollection.MyAction);

api.RegisterFunc("MyFunction", PluginCollection.MyFunction);


// Provided as lambda
api.RegisterAction("MyAction", (aApi, args) =>
{
    Console.WriteLine($"Database Type: \"{aApi.DbType}\"");
    Console.WriteLine($"Parameter: {args[0]}");
});

api.RegisterFunc("MyFunction", (fApi, args) => (int) args[0] * 2);
```

Bean Plugins are registered by using `RegisterBeanAction()` or `RegisterBeanFunc()`

```csharp
var api = new BeanApi(myConnection);

// From a static class
api.RegisterBeanAction("MyBeanAction", PluginCollection.MyBeanAction);

api.RegisterBeanFunc("ReverseBeanKind", PluginCollection.ReverseBeanKind);

// Provided as lambda
api.RegisterBeanAction("MyBeanAction", (aBean, args) =>
{
    Console.WriteLine($"The Bean is of kind: \"{aBean.GetKind()}\"");
    Console.WriteLine($"Parameter: {args[0]}");
});

api.RegisterBeanFunc("ReverseBeanKind", (aBean, args) =>
{
    var chArr = aBean.GetKind().ToCharArray();
    Array.Reverse((Array) chArr);
    return new string(chArr);
});
```

### Invoking Plugins

The registered Plugins are invoked as follows. While the "normal" Plugins only receive the parameters, the Bean Plugins additonaly get the Bean passed in on which an operation is going to be done.

```csharp
// "normal" Plugins
api.Invoke("MyAction", "Param1");
var result = api.Invoke("MyFunction", 2);

// Bean Plugins
var bean = Load("Bean", 42)
api.Invoke("MyBeanAction", bean, "Param1");
var result = api.Invoke("ReverseBeanKind", bean);
```



## Hive

In some cases you need to keep information at a central place in order to access it from Oberservers or a Plugin. Here comes the `Hive` into play. The `Hive` is a simple `Dictionary<string, object>` just like the Properties of a Bean. The key value pairs of the `Hive` are accessed via API.

```csharp
// setting a Hive property
Api.Hive["myGlobalProp"] = "Hello World!";

// getting the value of a property
var globalProp = Api.Hive["myGlobalProp"];

// deleting a property
Api.Hive.Delete("myGlobalProp");

// Reset all properties to null
Api.Hive.ClearAll();

// Check if a Property exists
var exists = Api.Hive.Exists("propImLookingFor")
```

The only implemented usage of the `Hive` is to set and get the current User information. This makes those (and any other) information available in any Observer and Plugin that's loaded into the API.



## BeanApi Object Lifetime

The `BeanApi` class implements `IDisposable` (it holds the `DbConnection`) and is not thread-safe. Care should be taken to ensure that the same `BeanApi` and `DbConnection` instance is not used from multiple threads without synchronization, and that it is properly disposed. Let's consider some common usage scenarios.

### Local Usage

If NBean is used locally, then it should be enclosed in a `using` block:

```cs
using(var api = new BeanApi(connectionString, connectionType)) {
    api.EnterFluidMode();

    // work with beans
}
```

### Global Singleton

For simple applications like console tools, you can use a single globally available statiс instance:

```cs
class Globals {
    public static readonly BeanApi MyBeanApi;

    static Globals() {
        MyBeanApi = new BeanApi("connection string", SQLiteFactory.Instance);
        MyBeanApi.EnterFluidMode();
    }
}
```

In case of multi-threading, synchronize operations with `lock` or other techniques.

### Web Applications (classic)

In a classic ASP.NET app, create one `BeanApi` per web request. You can use a Dependency Injection framework which supports per-request scoping, or do it manually like shown below:

```cs
// This is your Global.asax file
public class Global : HttpApplication {
    const string MY_BEAN_API_KEY = "bYeU3kLOQgGiWqUIql7Hqg"; // any unique value

    public static BeanApi MyBeanApi {
        get { return (BeanApi)HttpContext.Current.Items[MY_BEAN_API_KEY]; }
        set { HttpContext.Current.Items[MY_BEAN_API_KEY] = value; }
    }

    protected void Application_BeginRequest(object sender, EventArgs e) {
        MyBeanApi = new BeanApi("connection string", SQLiteFactory.Instance);
        MyBeanApi.EnterFluidMode();
    }

    protected void Application_EndRequest(object sender, EventArgs e) {
        MyBeanApi.Dispose();
    }

}
```

### ASP.NET Core Applications

Subclass `BeanApi` and register it as a **scoped** service in the Startup.cs file:

```cs
public class MyBeanApi : BeanApi {
    public MyBeanApi()
        : base("data source=data.db", typeof(SqliteConnection)) {
        EnterFluidMode();
    }
}

public class Startup {
    public void ConfigureServices(IServiceCollection services) {
        // . . .
        services.AddScoped<MyBeanApi>();
    }
}
```

Then inject it into any controller:

```cs
public class HomeController : Controller {
    BeanApi _beans;

    public HomeController(MyBeanApi beans) {
        _beans = beans;
    }

    public IActionResult Index() {
        ViewBag.Books = _beans.Find("book", "ORDER BY title");
        return new ViewResult();
    }
}
```



## Internal Query Cache

Results of all recent read-only SQL queries initiated by [finder](#finding-beans-with-sql) and [generic query](#generic-sql-queries) functions are cached internally on the *least recently used* (LRU) basis. This saves database round trips during repeated reads.

The number of cached results can be adjusted by setting the `CacheCapacity` property:

```cs
// increase
api.CacheCapacity = 500;

// turn off completely
api.CacheCapacity = 0;
```

Cache is fully invalidated (cleared) on:

- any non-readonly query (UPDATE, etc)
- failed [transaction](#transactions)

In rare special cases you may need to **bypass** the cache. For this purpose, all query functions provide overloads with the `useCache` argument:

```cs
var uid = api.Cell<string>(false, "select hex(randomblob(16))");
```