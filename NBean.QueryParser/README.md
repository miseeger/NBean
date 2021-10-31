

![NBeanLogo](Assets/NBeanLogoLs_md.png)

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



## License

This code is maintained and further developed with :heart: by [Michael Seeger](https://github.com/miseeger) in Germany. Licensed under [MIT](https://github.com/miseeger/NBean/blob/main/LICENSE.txt).
