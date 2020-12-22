

![NBeanLogo](Assets/NBeanLogoLs_md.png)



![build](https://img.shields.io/badge/build-successful-success) ![build](https://img.shields.io/badge/tests-passed-success) ![coverage](https://img.shields.io/badge/coverage-90%25-green)  [![net](https://img.shields.io/badge/netstandard-2.0-blue)](https://dotnet.microsoft.com/platform/dotnet-standard)  [![lic](https://img.shields.io/badge/license-MIT-blue)](https://github.com/miseeger/NBean/blob/main/LICENSE.txt)  ![ver](https://img.shields.io/badge/version-2.0.4.preview-informational)



NBean is a fork of [LimeBean](https://github.com/Nick-Lucas/LimeBean) with a couple of additional features, exclusively targeting NetStandard 2.0. It is a highly [RedBeanPHP](http://redbeanphp.com/)-inspired ORM for .NET which provides a simple and concise API for accessing **ADO.NET** data sources. It's an **Hybrid-ORM** ... halfway between a micro-ORM and plain old SQL.

Supported databases include:

- **SQLite**
- **MySQL/MariaDB**
- **PostgreSQL**
- **SQL Server**



## ... but ... why ... another ORM?

You are probably asking: "But why revive an apparently abandoned library and why an ORM of all things when there are so many ORMs out in the wild?" Well, for one thing, the open source idea lives deep within me and I'm simply fascinated by LimeBean and its role model RedBeanPHP. But that didn't just happen. After developing a PHP project with the briliant [Fat Free Framework](https://fatfreeframework.com) and its [Smart SQL ORM](https://fatfreeframework.com/3.7/databases#TheSmartSQLORM), I got so used to the dynamic character of PHP and this library (with all its advantages and disadvantages) that I somehow have missed this approach in my .NET projects.
By chance I stumbled upon LimeBean and immediately fell in love with this Hybrid-ORM. After a short contact with the current maintainer of LimeBean, I made the decision to fork the project and continue under my own "branding" and start this open source and also learning adventure. And that's pretty much it.



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

// Store it
// Store() will Create or Update a record intelligently
var id = api.Store(bean);

// You can also use this shortcut and chain .Put() and .Store() to do this
var id2 = api.Dispense("book")
    .Put("title", "The Art Of War")
    .Put("rating", 10)
    .Store();

// Store also returns the Primary Key for the saved Bean, even for multi-column/compound keys
Console.WriteLine(id);
Console.WriteLine(id2);
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

Short version:

```csharp
api.Load("book", id)                                  // <-- Load a Bean with a known ID
    .Put("release_date", new DateTime(2015, 7, 30))   // <-- make some edits
    .Put("rating", 5)
    .Store();                                         // <-- update database
```

> **Note:** This version doesn't check for not found Book Bean

**Delete**

```cs
api.Trash(bean);

// or

api.Load("book", id).Trash();
```



## Typed Accessors

To access bean properties in a strongly-typed fashion, use the `Get<T>` method:

```cs
bean.Get<string>("title");
bean.Get<decimal>("price");
bean.Get<bool?>("someFlag");
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



## Pagination

Pagination is one of the most important concepts when building Web APIs and so NBeans also privides Methods to deliver chunks of a result set of retrieved Beans, split in pages. NBeans comes with two styles of pagination: The "Beans Style" only returning a page of Beans as an Array of Beans (or Custom Beans) and the "Laravel Style" where the pagination method returns an object containing the Array of Beans and meta information like current page, max page number, etc. The Pagination Methods are provided by the `BeanApi` class.

The signature of any pagination method is identical and each method receives the following parameters (some of them have deafault values):

```csharp
Paginate(bool useCache, string kind, int pageNo, int perPage = 10, 
        string propsIgnorelist = "", string expr = null, 
        params object[] parameters)
Paginate<T>(bool useCache, int pageNo, int perPage = 10, 
        string propsIgnorelist = "", string expr = null, 
        params object[] parameters)
LPaginate(bool useCache, string kind, int pageNo, int perPage = 10, 
        string propsIgnorelist = "", string expr = null, 
        params object[] parameters)    
```

There are also convenience methods that come without the `useCache` parameter. They will use the Query Cache by default:

```csharp
Paginate(string kind, int pageNo, int perPage = 10, 
        string propsIgnorelist = "", string expr = null, 
        params object[] parameters)
Paginate<T>(int pageNo, int perPage = 10, 
        string propsIgnorelist = "", string expr = null, 
        params object[] parameters)
LPaginate(string kind, int pageNo, int perPage = 10, 
        string propsIgnorelist = "", string expr = null, 
        params object[] parameters)
```

Here is a short explanation of the parameters with an Example:

| Parameter         | Meaning                                              | Default value | Example                              |
| ----------------- | ---------------------------------------------------- | ------------- | ------------------------------------ |
| `useCache`        | Use Query Cache                                      |               | false                                |
| `kind`            | Bean Kind                                            |               | "Foo"                                |
| `pageNo`          | Number of page to return                             |               | 3                                    |
| `perPage`         | Number of Beans per page                             | 10            | 25                                   |
| `propsIgnoreList` | Bean Properties to omit                              | ""            | "id,Bar"                             |
| `expr`            | Query Expression (also <br />contains the sort oder) | ""            | WHERE Bar={0} <br />ORDER BY id DESC |
| `parameters`      | Parameters for the Query <br />Expression            | null          | "Baz"                                |

### Plain Paginating

The "plain" pagination returns an Array of Beans or an Array of Custom Beans in the defined portion.

```csharp
var employeePageOfBeans = 
    api.Paginate(true, "Employee", 2, 25, "Email,Phone",
        "WHERE Department = {0} ORDER BY Lastname, Firstname", 
        "Asset Management");
```

This returns the second page of the retrieved result set of employees working in the "Asset Managment" department, ordered by lastname and firstname. The page holds 25 employees (at max) and the returned Employee Beans do not contain Email and Phone.

### Paginating The Laravel Style

As explained above the Laravel Style not just returns an Array of Beans. It also provides meta informations, as the PHP Framework Laravel does. The object containing the returned values is defined as follows:

```csharp
public class Pagination
{
    public IDictionary<string, object>[] Data { get; set; } // Array of retrieved Beans
    public long Total { get; set; } // Total number of Beans in the Query
    public int PerPage { get; set; } // Beans per page
    public int CurrentPage { get; set; } // current page
    public int LastPage { get; set; } // last page
    public int NextPage { get; set; } // next page (-1 if current page = last page)
    public int PrevPage { get; set; } // previous page (-1 if current page = first page)
    public long From { get; set; } // sequence number of first Bean on page
    public long To { get; set; } // sequence number of last Bean on page
}
```

The `Pagination` object shows the current state of a delivered page and the data portion. 

```csharp
var employeePageOfBeans = 
    api.LPaginate("Employee", 3, 4, "Email,Phone",
        "WHERE Department = {0} ORDER BY Lastname, Firstname", 
        "Asset Management");
```

> To use the paginated data in a Web API it can be easily serialized as JSON by executing the `ToJson()` Extension Method.

Example Output of the above command, serialized to JSON:

``` json
{
  "data": [
    {
      "id": 89,
      "firstname": "Rafael",
      "lastname": "Byrd",
      "department": "Asset Management",
      "city": "Balsas",
      "startDate": "2019-05-22T20:43:36-07:00"
    },
    {
      "id": 95,
      "firstname": "Aidan",
      "lastname": "Hardin",
      "department": "Asset Management",
      "city": "Roma",
      "startDate": "2015-11-19T06:59:12-08:00"
    }
  ],
  "total": 10,
  "perPage": 4,
  "currentPage": 3,
  "lastPage": 3,
  "nextPage": -1,
  "prevPage": 2,
  "from": 9,
  "to": 10
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



## Importing, Exporting, Copying and Cleansing

With Beans it is possible to export the data portion (properties) and import data to a bean's properties. There's also a way to "cleanse" a bean's properties to ignore them when exporting or when cleansing the bean itself.

### Import

To data to a bean in order to either seed an empty bean that was just dispensed or override already existing property values there's the `Import` method which takes an `IDictionary<string, object>` and writes it into the bean.

```csharp
var newBean = Api.Dispense("foo")
    .Import(
        new Dictionary<string, object>()
        {
            {"Bar", 1},
            {"Baz", "Bang"}
        }
    );
```

### Export

When exporting the data portion of a bean, by default all the properties and their data is returned from the `Export()` method as an `IDictionary<string, object>`. It is also possible to omit/ignore certain properties by reaching them as a comma separated string to the method.

```csharp
var bean = Api.Dispense("foo")
    .Import(
        new Dictionary<string, object>()
        {
            {"id", 1},
            {"Bar", 12},
            {"Baz", "Bang"}
        }
    );

var data = bean.Export("id")

// Result = { {"Bar", 1}, {"Baz", "Bang"} }
```

So it will be possible to extract confidential data portions from a bean when exporting its properties.

### Copy

In some cases it comes in handy to just copy a bean resulting in a new Bean Object that contains all the properties of its original or just a part of its data by applying a `propsIgnorelist` to the `Copy()` method.

```csharp
var bean = Api.Load("foo", 1);

var copy = bean.Copy(); // creates an identical copy from `bean`.
var copy = bean.Copy("id") // creates a copy from `bean`, omitting the `id` Property
```

### Cleanse and FCleanse

Deleting some Properties from a Bean can be done by using the `Cleanse()` method which receives a `propsIgnoreslist` containing all the Properties (names) as a comma separated list. The `Cleanse()` method chganges the Bean Object directly without returning anyting. If "cleansing" a Bean should be done in a fluent manner the `FCleanse()` method must be used. It returns the cleansed Bean Object.

```csharp
var bean = Api.Dispense("foo")
    .Import(
        new Dictionary<string, object>()
        {
            {"id", 1},
            {"Bar", 12},
            {"Baz", "Bang"}
        }
    );

bean.Cleanse("Baz"); // <-- removes the `Baz` Property from `bean`

var bean = Api
    .Load("foo", 1)
    .FCleanse("id")); // <-- removes `id` from the loaded bean

```

There is also an `FCleanse<T>` method to cleanse Custom Beans.



## JSON

Serializing objects to JSON (Strings) is a standard requirement to any library that handles with data and so NBeans provides methods to either deliver JSON data from the BeanApi, from a Bean directly or via Extension Methods. To achieve this, NBean uses the System.Text.Json Namespace. JSON serialized objects can be formatted with indentations to make them look prettier and are always delivered with camelCase property names.

### `ToJson()` Api methods

The BeanApi as the top level interface layer provides two `ToJson()` Methods that serialize Beans or IEnumerables of Beans to a JSON string. Each method can be provided with a `propsIgonrelist` to omit  confidential properties when serializing. A second parameter (`toPrettyJson`) determines if the JSON string should be indented.

```csharp
var bean = api.Load("foo", 1);
// prints the unfiltered bean as a non-indented string
Console.WriteLine(api.ToJson(bean)); 
// pretty prints the loaded bean, omitting the `id`
Console.WriteLine(api.ToJson(bean, "id", true)); 

var beans = api.Load("foo", "WHERE Baz LIKE '%an%'");
var plainJson = api.ToJson(beans);
var prettyFilteredJson = api.ToJson(beans, "id,Bar", true);
```

### `ToJson()` Extension method

Since NBean basically handles with Objects (Beans are at least Objects :wink:) there is also at least only one multi purpose `ToJson()` method that is implemented as an Extension Method to the type `object`. It serializes any `object` to Json and either pretty prints it or not. The downside is that it can not be instructed to e. g. return a serialized Bean with ignored properties. This has to be done prior to calling `ToJson()` in a fluent manner.

```csharp
// just serializing
var json = api.Load("foo", 1).ToJson();

// serializing a Bean and omitting `id`
var json = api.Load("foo", 1).ToJson("id");

// serializing a bunch of beans, pretty printed
var beans = api.Load("foo", "WHERE Baz LIKE '%an%'").ToJson("", true);

// serializing a bunch of beans, pretty printed, omitting `id`
var beans = api.Find("foo").Select(c => c.Export("id")).ToJson(true);
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
var rows = api.Rows(@"SELECT 
                          author 
                          ,COUNT(*) 
                      FROM 
                          book 
                      WHERE 
                          rating > {0} 
                      GROUP BY 
                          author", 7);

// Load a single row
var row = api.Row(@"SELECT 
                        author
                        ,COUNT(*) 
                    FROM 
                        book 
                    WHERE 
                        rating > {0}
                    GROUP BY 
                        author 
                    ORDER BY 
                        COUNT(*) DESC 
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



## Using [Sequel](https://github.com/pimbrouwers/Sequel) SQL Builder For Generic Queries

Instead of typing generic queries or CUD-Commands completely by hand, it is possible to use the functionality provided by Sequel SQL builder. NBean was extened by Methods that execute SQL statements built with Sequel in order to fetch Rows, Columns, Scalar values or paginated Rows.

```csharp
// SELECT * FROM Product
result = new SqlBuilder()
    .Select("*")
    .From("Product")
    .Fetch(_api); // fetches Rows

// SELECT Name FROM Product
var result = new SqlBuilder()
    .Select("Name")
    .From("Product")
    .FetchCol<string>(_api); // fetches a single column's values

// SELECT COUNT(*) AS Cnt FROM Product
var result = new SqlBuilder()
    .Select("COUNT(*) AS Cnt")
    .From("Product")
    .FetchScalar<int>(_api); // fetches a scalar value
```



>  **Any SQLBuilder Extension Method needs at least a BeanApi instance to be passed.**



It is also possible to pass parameters to be used in a WHERE-Clause

```cs
// SELECT COUNT(*) AS Cnt FROM Product WHERE InStock >= 5
var result = new SqlBuilder()
    .Select("COUNT(*) AS Cnt")
    .From("Product")
    .Where("InStock >= {0}")
    .FetchScalar<int>(_api, 5);
```

The Query results can be paginated (in this example a T-SQL Query)

```csharp
// Get the 3rd page of active products, 10 per Page
// SELECT * FROM Product WHERE Active = 1 OFFSET 20 ROWS FETCH NEXT 10
result = new SqlBuilder()
    .Select("*")
    .From("Product")
    .Where("Active = 1")
    .FetchPaginated(_api, 3, 10);
```

Any other SQL command (Insert, Update, Delete, etc.) can be executed by using the `Execute` Extension Method.

```csharp
// Inserts a new Product
var result = new SqlBuilder()
    .Insert("Product")
    .Into("Id", "Name")
    .Values("6", "'High Power Gamer Notebook'")
    .Execute(_api);
```

For further information about the Sequel Query Builder, head over to the [project page on GitHub](https://github.com/pimbrouwers/Sequel).



## The Query Parser

The `UrlQueryParser` helper class in conjunction with some String Extension Methods provides a tool set to create secure terms for the WHERE clause and secure sequences for the ORDER-BY clauses of an SQL Query command. This parser comes in handy when filters must be passed from a url querystring to an API endpoint. So `[(]Foo:EQ{Bar} [AND] Baz:NE{12}[)] [OR] Zap:ISNULL`, for example, will be parsed to `( Foo = {0} AND Baz <> {1} ) OR Zap IS NULL`. The given values are packed into a `object[]` in order to pass the parameters along to the `Rows()` method. An ORDER-BY clause based on `Foo:ASC, [Bar]:DESC`  will result in `Foo ASC, [Bar] DESC` .

Under the hood the `UrlQueryParser` uses RegEx to sanitize and tokenize a given filter string or order string: It then parses the tokens into valid SQL, extracting the parameter values and converts them from string into valid types.

### Parsing Query filters from String

A filter term consists of one or more simple SQL expressions that are "connected" by logibal operators and may be separated into logical groups by using braces.

#### Expressions

An Expression is built by a fieldname, followed by a colon that separates it from the comparison operator and the given value(s). It may also possible that no value is needed (e. g. in the case of `IS NULL`).

```
<Fieldname>:<ComparisonOperator>[{<Value1[,Value2]..[,ValueN]}]
```

The possible comparison operators are:

| Operator (alias) | Function         | Resulst In            |
| ---------------- | ---------------- | --------------------- |
| `EQ`             | equals           | =                     |
| `NE`             | not equal        | <>                    |
| `GT`             | greater than     | >                     |
| `GE`             | greater or equal | >=                    |
| `LT`             | less than        | <                     |
| `LE`             | less or equal    | <=                    |
| `LIKE`           | like             | LIKE                  |
| `BETWEEN`        | between          | BETWEEN .. AND ..     |
| `NOTBETWEEN`     | not between      | NOT BETWEEN .. AND .. |
| `IN`             | in               | IN (...)              |
| `NOTIN`          | not in           | NOT IN (...)          |
| `ISNULL`*        | is NULL          | IS NULL               |
| `ISNOTNULL`*     | is not NULL      | IS NOT NULL           |

*Needs no comparison value.

Each comparison operator has to be noted in capital letters. The `UrlQueryParser` is case-sensitive.

The parameters/values for a comparison are noted between curly braces, if more than one value is needed (e. g. for `BETWEEN` or `IN`) the values must be separated by comma. There is no space needed after the comma. Strings and DateTime values don't need to be quoted.

#### Logic Operators

Multiple expressions are "connected" by logic operators (`AND`, `OR` and `NOT`). Logic operators are put into square brackets: `[AND]`, `[OR]`, `[NOT]`

#### Braches

Braces are used to group logic operations and are also put into square brackets: `[(]`,  `[)]`.

#### Values / Parameters

Values (Parameters) are put right after the comparison operator without any whitespaces. They are put into curly braces (`{}`) and separated by comma if multiple values are needed.

```
{12}
{18,70}
{Bar:NOTIN{Baz,Bang,Bong}
```

Each value will be replaced by a parameter (`{0}..{m}`), converted to a valid type and put into an Array of `object`.

#### Prevented SQL Injection

Since the UrlQueryParser works based on RegEx patterns it only takes valid expressions and parses them. Any value is replaced by a parameter placeholder, converted and put into the prameter array. This pretty much prevents SQL Injection. As a pre-security step the given query string will be sanitized before the parsing is started.

Bad query strings like this one ...

```sql
[Foo]:NOTBETWEEN{18,70} [AND] Bar:IN{Baz,Bang,Bong}; DROP TABLE USERS;
```

... will be sazitized to become ...

```sql
[Foo]:NOTBETWEEN{18,70} [AND] Bar:IN{Baz,Bang,Bong}
```

Expressions that are always true will also be prevented:

```sql
[Foo]:NOTBETWEEN{18,70} [OR] 1:EQ{1}
```

... becomes ...

```
[Foo]:NOTBETWEEN{18,70}
```

#### Examples (taken from `UrlQueryParserTests` )

```sql
[(]Foo:EQ{Bar} [AND] Baz:NE{12}[)] [OR] Zap:ISNULL
... becomes ...
( Foo = {0} AND Baz <> {1} ) OR Zap IS NULL
-> Parameters: ["Bar",12]

Foo:GT{18} [AND] [NOT] Bar:LT{70} [AND] Baz:ISNOTNULL
... becomes ...
Foo > {0} AND NOT Bar < {1} AND Baz IS NOT NULL
-> Parameters: [18,70]
  
[Foo]:BETWEEN{18,70} [AND] Bar:NOTIN{Baz,Bang,Bong}
... becomes ...
[Foo] BETWEEN {0} AND {1} AND Bar NOT IN ({2},{3},{4})
-> Parameters: [18,70,"Baz","Bang","Bong"]
```

### Parsing Order commands from String

`UrlQueryParser` also supports the parsing of ORDER-BY commands. The method is also based on RegEx and processes only order tokens that match this pattern:

```
<Fieldname1>:<ASC|DESC>,..,[<FieldnameN>:<ASC|DESC>]
```

The fieldname of the sored field is followed by a colon and the direction to sort (either `ASC` or `DESC`). One of both has to be given otherwise the order segment is omitted. When parsing the ORDER sequence the given string is first sanitized and then only the colons are replaced by a space character.

#### Example (taken from `UrlQueryParserTests`)

```
Foo:ASC, [Bar]:DESC, Baz
... becomes ...
Foo ASC, [Bar] DESC
```



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



## Data Validation with Custom Bean Classes

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



## Bean Validation using the Validator

NBean contains a  `Validator` class which is based on a LINQ micro rule engine that makes it possible to validate a Bean against a set of defined rules. A rule is represented by an instance of a `BeanRule` object.

```csharp
var firstRule = new BeanRule()
{
    Test = (b) => b.Get<string>("Name").Length <= 16,
    Message = "Name is too long (max. 16 characters)."
},
```

The `Test` is declared by defining a lambda which returns a boolean, getting the Bean passed which is to be tested. One or multiple BeanRules can be added to the `Validator` for a certain kind of Bean. The rules can be added on creation or after creation (using the `AddRule()` or `AddRules()` method).

### Setting up the Validator

Using the constructor with a list of BeanRules

```csharp
validator = new Validator(new Dictionary<string, BeanRuleList>()
    {
        {
            "MyBean", new BeanRuleList()
            {
                new BeanRule()
                {
                    Test = (b) => true,
                    Message = "You shall always pass!"
                },
                new BeanRule()
                {
                    Test = (b) => false,
                    Message = "You shall never pass!"
                }
            }
        }
    }
);
```

Using the `AddRule()`-Method

```csharp
var validator = new Validator();

validator.AddRule("MyBean",
    new BeanRule()
    {
        Test = (b) => true,
        Message = "You shall always pass!"
    }
);
```

### Validating a Bean

As shown above, one Rule or a set (list) of Rules has/have to be defined and registerd with the `Validator` for a given Bean kind, in order to vaildate Beans of this kind.

```csharp
var validator = new Validator();

validator.AddRules("TestBean",
    new BeanRuleList()
    {
        new BeanRule()
        {
            Test = (b) => b.Get<string>("Name").Length <= 16,
            Message = "Name is too long (max. 16 characters)."
        },
        new BeanRule()
        {
            Test = (b) => b.Get<long>("Value") >= 18 && b.Get<long>("Value") <= 66,
            Message = "Value must be between 18 and 66."
        }
    }
);
```

To validate a Bean of kind `TestBean` just call `Validator.validate("TestBean")`.

```csharp
var testBean = api.Dispense("TestBean")
    .Put("Name", "This is my veeeery long name")
    .Put("Value", 42);

var (result, message) = validator.Validate(testBean);
```

The result of the validation is a `Tuple` that contains the `result` (`bool`) and the messages of the failed rules, separated by "CRLF". The return value of the validation, shown above would be:

```csharp
result = false // <-- validation failed
message = "Name is too long (max. 16 characters).\r\n" //  <-- error message(s)
```



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

Another convention based way to work with 1:n and m:n relations in NBeans does not need any Custom Bean Classes but this way also supports Custom Beans.

### 1:n Relations

#### Conventions for 1:n relations

The "n" part of this relational type is called "owned" (also referencing) Bean. This Bean stores the Foreign Key that references the "1" part of this relation, the "owner" (referenced) Bean. The name of the Foreign Key that is pointing to the Primary Key of the owner is built by the Kind (name) of the referenced Bean suffixed by an underscore (`_`) and followed by the name of its Primary Key name, e.g.: `Contact_id` or `Activity_id`. This Foreign Key must be defined as column of the referencing Bean's table in the database.

```sql
CREATE TABLE [Activity] (
    id INTEGER NOT NULL PRIMARY KEY,
    Description VARCHAR(64),
    ...
    Contact_id INTEGER  -- Foreign Key that points to the Contact, owning the Activity
)
```

> It is absolutely necessary to respect the case sensitivity and correct spelling of the Bean Kind that corresponds to the first part of the Foreign Key Name. Misspelling or ignoring case sensitivity may end up in various problems.

#### Attaching to an owner 

An 1:n relation is established by "attaching" the owned Bean to the Owner. It is released by "detaching" the owner Bean (Kind).

```csharp
// get the owner Bean
var contact = _api.Load("Contact", 12);

// attach an existing Bean
var existingActivity = _api.Load("Activity", 123);
contact.AttachOwned(existingActivity);

// or short
var contact = _api.Load("Contact", 12);
contact.AttachOwned(_api.Load("Activity", 123));

// or even shorter
_api.Load("Contact", 12).AttachOwned(_api.Load("Activity", 123));

// attaching a newly dispensed Bean will automatically store it
var contact = _api.Load("Contact", 12);
var newActivity = _api
    .Dispense("Activity")
    .Put("Description", "Coffee break!");
contact.AttachOwned(newActivity);
```

Attaching owned Beans is not only limited to one Bean at a time. It is also possible to attach a list of Beans. When attaching Beans they may be either loaded or newly dispended.

```csharp
var contact = _api.Load("Contact", 12);
var activityList = new List<Bean>()
{
    // existing activities
    _api.Load("Activity", 4711),
    _api.Load("Activity", 121),
    // new activity
    _api.Dispense("Activity").Put("Description", "Lunch break!")
};

contact.AttachOwned(activityList);
```

Attaching to an owner can also be made vice versa (from the "n" side of the relation). The use case may be to change the Owner of an activity or relate an "orphaned" activity which currently has no owner to a new owner.

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

It is also possible to get information about the related owner from the "n" side of the relation using the `GetOwner()` method or `GetOwner<T>()` method for Custom Beans. In this case it is only needed to know the Kind or type of the Bean that is to retrieve.

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
contact.DetachOwned(myActivities.FirstOrDefault(), true) // <-- deletes the activity - you may also trash this bean ;-) 
    
// detach list of all "lunch" activities of the contact and leaves them "orphaned"
contact.DetachOwned(myActivities.Where(ma => ma.Get<string>("Description").Contains("Lunch")));

// detaches all "coffee" activities of the contact by deleting them
contact.DetachOwned(myActivities.Where(ma => ma.Get<string>("Description").Contains("Coffee")), true);
```

Detaching  from an owner can also be made vice versa (from the "n" ("owned") side of the relation), using the `DetachOwner()` method.  As with the other detach methods it's also possible to delete the owned Bean or leave it as "orphaned". There is also support for Custom Beans.

```csharp
var activity = _api.Load("Activity", 123);

activity.DetachOwner("Contact");       // -- leaves the activity "orphaned"
activity.DetachOwner("Contact", true); // -- deletes the activity

// using Custom Beans
activity.DetachOwner<Contact>();
activity.DetachOwner<Contact>(true);
```

#### Foreign Key Alias

Imagine you want to relate an owned Bean to more than one Owner (related Bean) because the Owner may have multiple meanings like the primary and secondary address for a contact. To implement this you will need more than one (here: two) Foreign Key Columns in the owned Bean's table (here: Contact) but those columns cannot be named the same. Sure one column could be named linke the conventions describe it. But the second one must have a different name. For the example "contact has two addresses" we would crate the Foreign Keys `PrimaryAddress_id` and `SecondaryAddress_id`. Both Foreign Key Columns cannot be found by the regular convention that just takes the Bean's Kind to create the Foreign Key. You will have to use an alias in this case to make the Foreign Keys distinctive and accessible. For this case all methods that are in the scope of 1:n relations support naming an alias. Here are some examples how to use them:

```csharp
// Assumption: Many contacts may have the same primary and many contacts may have the same secondary address

// Attaching
var address1 = _api.Load("Address", 1);
var address2 = _api.Load("Address", 4711);

var contact = _api.Load("Contact", 1);

address1.AttachOwned(contact, "PrimaryAddress");
address2.AttachOwned(contact, "SecondaryAddress");


var contactsAtThisPrimAddress = address1.GetOwnedList("Contact", "PrimaryAddress");
var contactsAtThisSecAddress = address2.GetOwnedList<Contact>("SecondaryAddress");

// Detaching
var contactList = address2.GetOwnedList("CONTACT", "SecondaryAddress").ToList();

bean1.DetachOwned(contactList[1], true, "SecondaryAddress");
bean1.DetachOwned(contactList[2], false, "SecondaryAddress");
```

### m:n Relations

m:n relations are implemented by using a link table in the background that stores the relations and establishes something like 1:n / n:1 under the hood. As we need this link table, we use the word "Link" for an m:n relation in NBeans. We are linking m Beans of Type X with n Beans of Type Y. For example Products that are sold in various stores or Products that are bought from different suppliers.  

#### Conventions for m:n relations

The link table will not be automatically generated from NBeans. It has to be created manually and adhere to a small number of conventions:

- The link table is named by putting together the names/kinds of the related beans, followed by `_link`. Be also aware of case sensitivity here! The Bean names can be put in any order.
- A link table must have a Primary Key field (`id`,  by default)
- Either parts of the relation are represented by Foreign Key Fields, put together by the name/kind of the Bean and `_id` as suffix. Where `id` is the name of the default key.
- Link tables may have further properties/columns in order to store link related information.
- Take into consideration that link tables are also audited, if the `Auditor` Observer is in use. It is suggested to blacklist them to reduce the verbosity of the `Auditor`. 

The following definition of a link table relates Stores and Products (the linke table may also be named `ProductStore_link`).

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

A link between two Beans is established from either part of the relation. It does not matter from which side to start. It is possible to link just one Bean or a list of Beans. Also Custom Beans can be linked among each other and to "regular" Beans. If the link table (Link Bean) has columns (Properties) that must be provided with data then it is possible to pass the data in a `Dictionary<string, object>`. **Note:** When linking a list of Beans, the provided link data is saved with each linked Bean. For each linked Bean it will be the same.

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

Retrieving a list of linked Beans from an m:n relation is achieved by calling the `GetLinkedList()` method from either side of the relation. This method comes also with a version for Custom Beans: `GetLinkedList<T>()`. These methods only provide the linked Beans. In order to get the additional link data that is stored with each link, the `GetLinkedListEx()` or `GetLinkedListEx<T>()` method must be called. Both return a `Dictionary` which Key is the linked Bean and Value is the Link Bean, containing the Foreigen Keys and link data.

```csharp
var store = _api.Load("Store", 1);
var storeProducts = store.GetLinkedList("Product");
var storeProductsExt = store.GetLinkedListEx("Product");

var customStore = _api.Load<Store>(1);
var customStoreProducts = cStore.GetLinkedList<Product>();
var customStoreProductsExt = cStore.GetLinkedListEx<Product>();
```

#### Unlinking Beans

Just like linking a Bean or a list of Beans it is possible to unlink a Bean or a list of Beans from an other linked Bean. Unlinking Beans can also be done from either part of the relation. It does not matter which side to unlink. Also Custom Beans can be unlinked. Unlinking Beans means to **delete** the Link Bean that holds the Link (m:n relation).

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

Auditing means tracking of changes in special ways. Out of the box NBean comes with two kinds of audit mechanisms, implemented as Observer Plugins. The `Auditor` Observer tracks any changes made with a Bean and logs them in a standard table called  `AUDIT` (capitalized!). Also new Beans and the deletion of beans are audited. The light version of auditing is provided by the `AuditorLight` Observer Plugin. It tracks creation and last change right into a bean if the needed properties (e. g. `CreatedAt` or `ChangedAt` ) are existing. Both Plugins can be used together but they work independently from each other.

### Current User

Auditing changes without the information who made the changes is pretty pointless. So the API provides a public Property `CurrentUser` that may be get and set. This information is taken to fill the user related fields of `Auditor` and `AuditorLight`. So be sure to set this Property a soon as the current user is known and immediately after the API is created. Independent from the audit functionality this `CurrentUser` can be used all over the system once the API is created.

### Auditor

The `Auditor` is an Observer Plugin that tracks changes made to Bean Properties (SQL UPDATE), creation of  Beans (SQL INSERT) and deletion of Beans (SQL DELETE). 

When the `Auditor` is instanciated in order to load it into the API it needs an API instance. As a second parameter a blacklist of Bean Kinds (database tables) can be provided to prevent auditing changes made tho those Beans. By default `AUDIT` is blacklisted. The Beans in this list are separated by semicolon (`;`). When instanciating the `Auditor` it is checked if the `AUDIT` table is existing. If not, it is created by the Observer.

> Even if the `AUDIT` table will be automatically created if it is not existing, it is highly recommended to create this table manually or by script because for creation the system will be put into [Fluid Mode](#fluid-mode).

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
    [id] INTEGER NOT NULL PRIMARY KEY,
    [AuditDate] DATETIME,
    [Action] VARCHAR(16),
    [User] VARCAHR(64),
    [Object] VARCHAR(64),
    [ObjectId] VARCHAR(64),
    [Property] VARCHAR(64),
    [PropertyType] VARCHAR(64),
    [OldValue] VARCHAR(1024),
    [NewValue] VARCHAR(1024),
    [Notes] VARCHAR(4096)
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

The existence of those fields is checked on UPDATE or INSERT, filled with the current user and current date and then saved with the changed or inserted data.

So just make sure the `AuditorLight` Observer is loaded and your tables have the needed columns and the Plugin just works.

> **Note:** It is important that the column names are spelled correctly. Please note the case-sensitivity.



## Slx Style Key Provider

Inspired by a CRM system called SalesLogix (now Infor CRM) there comes an Observer that provides auto incremental keys and theoretically generates 36^10 (more than 3.65 quadrillion) unique Keys until there's an overflow. This number is reduced to about 2.65 quadrillion because of the starting Key, as you can read below.

An Slx style key is composed by a prefix which has five characters representing the table or bean name, either full if the original name has just five or less characters or reduced by the vowels. Names shorter than five characters are filled with `#`. A an example look at the following prefixes that are generated according to this 

logic:

| Bean name    | Prefix |
| ------------ | ------ |
| foo          | FOO##  |
| foobar       | FBR##  |
| foobarb      | FBRB#  |
| foobarbaz    | FBRBZ  |
| foobarbazetc | FBRBZ  |

This prefix is followed by a dash `-` and the auto incremented part of the key follows after this. It has a length of 10 and starts with `A000000000`. So the first 10 x 36^9 Keys (about one quadrillion) can not be allocated. The Hex-System counts from `0` to `F`. This system goes a couple of steps beyond. It counts from `0` to `Z` and that makes 36 values instead of 16. Examples for Slx Style keys are:

```
FOO##-A000000010
FOO##-A0000001A0
FOO##-B000000000
```

The `SlxStyleKeyProvider` is added to the Observers of an API instance and works with the default Primary Key Property (system default is `id`). The Constructor will receive the instance of the API. This is the reason why it can not be added to the initial Observers of an API. 

```csharp
var api = new BeanApi(myConnection);
api.AddObserver(new SlxKeyProvider(api));
```



## Plugins

Plugins extend the system with Functions and Actions (Delegates / Lambdas) that are registered in the API and invoked by their name, passing the needed parameters. As Actions they return nothing, just doing things. Functions will return a value.

### Types of Plugins

There are two types of Plugins. First there are Plugins that work only on API level. Those are the "normal" Actions and Functions.  Second there are Plugins (Actions and Functions) that work with Beans. This also includes the ability to access the API via `bean.Api`.

Usually Plugins are placed inside a static class from which they are registerd at the API. The following two sections give an example of a "normal" Action and Function and a Bean Action and Bean Function 

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

In order to get the Bean Plugins properly working, the signature is mandatory: `Bean` must be passed at first place, followed by `params object[]`.

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

In some cases you need to keep information at a central place in order to access it from Oberservers or  Plugins. Here comes the `Hive` into play. The `Hive` is a simple `Dictionary<string, object>` just like the Properties of a Bean. The key value pairs of the `Hive` are accessed via API.

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

The only implemented usage of the `Hive` is to set and get the current User information. This makes this (and any other) information available for any Observer and Plugin that's loaded into the API. But this is not explicitly limited to Observers and Plugins. The information shared by the Hive can be accessed from any place in code where the API instance (holding the Hive information) is accessible from. 



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



## License

This beautiful ORM is maintained and further developed with :heart: by [Michael Seeger](https://github.com/miseeger) in Germany. Licensed under [MIT](https://github.com/miseeger/NBean/blob/main/LICENSE.txt).

