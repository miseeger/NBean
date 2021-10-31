

![NBeanLogo](Assets/NBeanLogoLs_md.png)

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

// Laravel style pagination is also available:
result = new SqlBuilder()
    ...
    .FetchLPaginated(_api, 3, 10)
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



## License

This code is maintained and further developed with :heart: by [Michael Seeger](https://github.com/miseeger) in Germany. Licensed under [MIT](https://github.com/miseeger/NBean/blob/main/LICENSE.txt).
