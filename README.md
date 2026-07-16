# MerlinORM

![MerlinORM Logo](logo.png)

[GitHub Repository](https://github.com/Squil11ams/MerlinXX-ORM)

MerlinORM is a lightweight .NET object mapping and data infrastructure library designed to simplify the process of converting database results into strongly typed objects.

Merlin automatically handles object population, type conversion, nested object mapping, and structured error reporting while maintaining a simple developer interface.

The name Merlin represents two ideas:

- The magic of automatically mapping and converting data between types.
- The Rolls-Royce Merlin engine, a symbol of engineering, reliability, and performance.

The goal of MerlinORM is not to hide complexity, but to provide a reliable engine that handles the complexity internally while exposing a clean and predictable API to developers.

[Merlin vs Dapper Performance]([https://github.com/Squil11ams/MerlinXX-ORM](https://github.com/Squil11ams/MerlinXX-ORM/wiki/Merlin-Vs-Dapper))


For complete documentation, examples, and advanced usage, see the Wiki:

https://github.com/Squil11ams/MerlinXX-ORM/wiki

---

# Looking Ahead 

Key improvements I'm focused on
- Performance
- AOT Compatible (Not sure how yet, may involve a standalone project where you feed models, or even better connect to database to generate models and output pre-compiled cache object to eliminate reflection.)
- Trimming Compatible
- MSSQL Support will have soon the project im working on that required this re-work doesnt us mssql.

## 🚀 MerlinORM Roadmap

### Performance

- [x] Metadata caching
- [ ] Cache SQL column ordinals (avoid repeated `reader["Column"]` lookups)
- [ ] Benchmark against Dapper and raw ADO.NET
- [ ] Reduce allocations during object mapping
- [ ] Cache compiled converters
- [ ] Optimize object creation pipeline

---

### Compatibility

- [ ] Native AOT compatibility
- [ ] Trimming compatibility
- [ ] PostgreSQL provider
- [ ] SQL Server provider
- [ ] SQLite provider

---

### Mapping

- [ ] Constructor-based mapping (records / immutable types)
- [ ] Custom value converters
- [ ] Enhanced enum mapping
- [ ] Ignore NULL database values option
- [ ] One-to-many collection mapping
- [ ] Multiple result set support

---

### Diagnostics

- [ ] Mapping performance statistics
- [ ] Configurable debug logging
- [ ] SQL execution timing
- [ ] Improved missing column diagnostics
- [ ] Mapping validation API
- [ ] Startup metadata validation

---

### Query API

- [ ] Parameter builder improvements
- [ ] Stored procedure helpers
- [ ] Bulk insert helpers
- [ ] Transaction helpers
- [ ] Async streaming (`IAsyncEnumerable<T>`)

---

### Developer Experience

- [x] XML documentation
- [ ] Additional sample projects
- [ ] SourceLink support
- [ ] Source generators (optional)
- [ ] Roslyn analyzers
- [ ] Expanded documentation and tutorials

---

# Guiding Principles

MerlinORM is designed to remain:

- Lightweight
- Explicit (SQL-first)
- Fast
- Predictable
- Easy to debug
- Easy to extend

---

# Not Planned

MerlinORM intentionally does **not** aim to become a full ORM like Entity Framework.

The following features are currently **not planned**:

- ❌ LINQ query provider
- ❌ Change tracking
- ❌ Lazy loading
- ❌ Automatic database migrations

Instead, MerlinORM focuses on providing a lightweight, high-performance mapping layer over ADO.NET while keeping developers in complete control of their SQL.


---

# Features

- Automatic database-to-object mapping
- Automatic type conversion
- Nested object population
- Attribute-based mapping configuration
- Custom property mapping
- Property exclusion
- Structured exception handling
- Resource-based safe error messages
- Detailed developer diagnostics
- Provider-based database architecture
- Extensible conversion engine

---

# Installation

Install the MerlinORM client package:

Database providers are installed separately.

Example MySQL provider:
---

# Basic Setup

MerlinORM uses three primary components:

1. A model definition
2. A configured database provider
3. A query execution request

---

# Creating a Model

Models should inherit from `MerlinObjectBase`.

Example:

```csharp
public class MyModel : MerlinObjectBase
{
    // col_id should be the actual name of the column in the database.
    public int col_id { get; set; }

    // col_name should be the actual name of the column in the database.
    public string col_name { get; set; }

    [AutoPopSettings("col_status")]
    public string Status { get; set; }

    [Exclude]
    public string NotInDatabase { get; set; }

    [MerlinObject]
    public MyOtherModel RelatedModel { get; set; }
}
```


## Exclude

`Exclude` prevents Merlin from attempting to populate a property.

Example:

```csharp
[Exclude]
public string CalculatedValue { get; set; }
```

This is useful for properties that are populated by application logic rather than database data.

---

## MerlinObject

`MerlinObject` enables nested object population.

Example:

```csharp
[MerlinObject]
public Address Address { get; set; }
```
Important, objects decorated with [MerlinObject] must implement the MerlinModelBase.

When enabled, Merlin will attempt to populate the nested object using the available data.

---

# Querying Data

Example Using the Model Above:

```csharp
public static class DataAccessLayer
{
    private static readonly QueryEngine DB = new("Default");

    public static MyModel GetMyModel(int id)
    {
        var query = new GenericQuery("SELECT A.*, B.* FROM MyModels JOIN OtherModel ON A.FK = B.ID WHERE col_id = @A;");

        query.AddParameter("@A", id);

        return DB.GetObject<MyModel>(query);
    }

    public static List<MyModel> GetMyModelList()
    {
        var query = new GenericQuery("SELECT A.*, B.* FROM MyModels A JOIN OtherModel B ON A.FK = B.ID;");

        return DB.GetList<MyModel>(query);
    }
}
```

---

# Type Conversion

Merlin includes a conversion engine designed to automatically handle common data conversions.

Supported conversions include:

* Database numeric types to compatible C# numeric types
* Nullable type handling
* String conversions
* Enum conversions
* Date and time conversions
* Custom conversion logic

When Merlin cannot complete a conversion, it provides detailed diagnostics.

Example:

```
MERLIN-CVT-0001

Failed to convert value.
```

Developer diagnostics may include:

```
Source Type:
String

Destination Type:
Int32

Value:
"ABC"
```

---

# Exception Handling

Merlin uses structured exceptions with unique error codes.

Example:

```
MERLIN-MAP-1029

[Mapping Engine]
Failed to populate model, see notes for more info.
```

Additional developer information is preserved separately:

```
'CustomerMapper'
failed to map property
'Age:Int32'
from
'Age:String'
```

The error code format is:

```
MERLIN-{MODULE}-{NUMBER}
```

Example:

```
MERLIN-MAP-1029
```

Where:

```
MAP = Mapping Module
1029 = Specific error condition
```

Merlin exceptions provide:

* A safe user-facing message
* Developer diagnostic information
* A unique searchable error code
* Inner exception preservation

---

# Database Providers

Merlin separates database functionality from the core mapping engine.

This allows database providers to be added without modifying the core library.

## Supported Providers

| Provider | Package         |
| -------- | --------------- |
| MySQL    | MerlinORM.MySQL |

Additional providers can be implemented using the Merlin provider architecture.

---

# Architecture

Merlin is designed around separation of responsibility:

```
MerlinORM.Client

    Mapping Engine
    Conversion Engine
    Exception Framework
    Core Infrastructure


MerlinORM.MySQL

    Database Provider
    Connection Handling
    Query Execution
```

The core engine does not depend on a specific database implementation.

---

# Advanced Features

For advanced usage see the Wiki:

* Custom converters
* Provider development
* Advanced mapping rules
* Configuration options
* Exception customization

Documentation:

https://github.com/Squil11ams/MerlinXX-ORM/wiki

---

# Version History

## 2.0.0

Merlin 2.0 represents a major architectural refinement focused on reliability, diagnostics, and extensibility.

### Added

* Structured exception framework
* Module-based error codes
* Resource-driven safe messages
* Improved mapping diagnostics
* Improved conversion handling
* Enhanced provider separation

### Changed

* Refactored internal architecture
* Improved exception reporting
* Improved maintainability and extensibility

### Breaking Changes

Applications upgrading from previous versions should review:

* Exception handling behavior
* Public API changes
* Provider integration changes

---

# License

See LICENSE for details.

