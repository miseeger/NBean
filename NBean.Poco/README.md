

![NBeanLogo](Assets/NBeanLogoLs_md.png)

## Simple Poco Mapping using [Mapster](https://github.com/MapsterMapper/Mapster)

Sometimes it can be handy to be able to map the data portion of plain Beans or resulting rows from a generic Query to a Poco. This is possible thanks to the magic of `Adapt<T>()` provided by Mapster. Mapster is a "*Kind of like AutoMapper, just simpler and way, way faster.*"

Mapping `Beans` and `IDictionary<string, object>` to Poco Objects does not make NBean a full ORM it just enables you to map to Poco in an easy an comfortable way. The only thing that is needed is the declaration of the Poco Class.

> This mapping functionality is just a mapping to and from simple Poco Objects. There is no support for nested Pocos.

The following examples are based on this very simple Poco Class:

```csharp
public class Poco
{
    public int? Id { get; set; }
    public int? A  { get; set; }
    public string B { get; set; }   
}
```

The Bean of kind "Bean" has adequate Properties.

### Map Bean and List of Beans to Poco (List of Pocos)

Mapping a Bean to a Poco Object is now "baked" into the `Bean` Class. Just use `ToPoco<T>()` and you're good to go. Since this Method is based on the Bean's `Export()` Method it also may receive a `propsIgnoreList` to suppress unwanted Properties.

Mapping a Bean:

```csharp
var bean = _api.Load("Bean", 123);
var poco = bean.ToPoco<Poco>();
var poco2 = bean.ToPoco<Poco>("Id,B"); // ignores Properties "Id" and "B"
```

There is also support to map a list/an array of plain Beans. It is provided by an Extension Method to `IEnumerably<Bean>`:

```csharp
var beans = _api.Find("Bean", "WHERE A > {0}", 4);
var pocos = beans.ToPoco<Poco>();
var pocos = beans.ToPoco<Poco>("B"); // ignores Property "B"
```

Afterwards you can do some LINQ stuff with `pocos`.

### Map Poco and List of Pocos to Bean (List of Beans)

Vice versa it is also possible to map a plain Bean or a list of plain Beans to a Bean or a list of Beans. It produces a Bean or a list of Beans of the given Bean Kind.

```csharp
var bean = poco.ToBean("Bean");

var beans = pocos.ToBeanList("Bean");
```

#### Importing from a Poco

The Bean Class provides an `Import()` Method that imports data portions / Property Values from a  `Dictionary<string, object>` to an existing Bean. It is also possible to Import new Data (Property Values) for an existing Bean from a Poco by using the `ImportPoco()` Method that ignores Null-Values.

```csharp
var bean = _api.Load("Bean", 123);

var poco new Poco
    {
        A = 42,
        B = "changed"
    };

bean.ImportPoco(poco);
bean.Store();
```

Importing the Poco object described above, the `Id` Property of the `bean` will not be overridden bedause `poco.Id` is Null. Only the Properties `A` and `B` are imported from `poco` because Null-Values are ignored when mapping.

### Map Row(s) to Poco(s), using generic Queries

When retrieving Rows from a generic Query you'll receive an IEnumerable<Dictionary<string, object>> and there is also a convenience Extension Method that maps such IEnumerables to a List of Poco objects. This may becoma handy when Querying data from multiple Tables or from a View where no explicit Bean comes into play. To map the Query result to a simple Poco / List of Pocos just use the `ToPoco()` Extension Method in order to use it as a ViewModel or the like.

```csharp
// fetching a single Row
var row = api.Row(@"SELECT TOP 1 author, COUNT(*) AS bookCount
                    FROM book 
                    GROUP BY author 
                    ORDER BY COUNT(*) DESC");

var authorWithMostBooks = row.ToPoco<BookCount>();                    
                    
// fetching multiple Rows
var rows = api.Rows(@"SELECT author, COUNT(*) AS bookCount
                      FROM book 
                      GROUP BY author
                      ORDER BY 2 DESC");

var authorBookCountList = rows.ToPocoList<BookCount>();
```



## License

This code is maintained and further developed with :heart: by [Michael Seeger](https://github.com/miseeger) in Germany. Licensed under [MIT](https://github.com/miseeger/NBean/blob/main/LICENSE.txt).

