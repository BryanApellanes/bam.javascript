# bam.javascript

JavaScript execution engine with embedded ECMAScript runtime, SQL provider integration, and schema generation from JavaScript object literals.

## Overview

The `bam.javascript` project is a .NET 10 class library (namespace `Bam.Javascript`) that provides server-side JavaScript execution using the EcmaScript.NET runtime. It enables running JavaScript code from strings or files, exposing .NET objects into the JavaScript scope via `CliProvider`, and extracting values back. It also includes an embedded `json2.js` resource for JSON serialization within the JavaScript context.

A key use case is reading JavaScript literal files (e.g., database schema definitions expressed as JS objects) and converting them to JSON for consumption by the Bam data layer. The `JsLiteralSchemaManager` class extends `DaoSchemaManager` to process table definitions and cross-references from parsed JavaScript objects, bridging the gap between JS-defined schemas and the Bam DAO generation pipeline.

The project also provides JavaScript minification (via YUI Compressor), a `NodeScriptRunner` for executing Node.js scripts as external processes, and a family of `JavaScriptSqlProvider` classes that expose database query execution to JavaScript contexts through a `[Proxy("sql")]` attribute. SQL providers exist for SQLite, MS SQL, Oracle, and PostgreSQL (the latter via stubs).

## Key Classes

| Class | Description |
|-------|-------------|
| `JsContext` | Core JavaScript execution context. Wraps an EcmaScript.NET `Context` and `ScriptableObject` scope. Supports loading scripts, setting/getting CLI values, and running scripts. |
| `CliProvider` | Pairs a variable name with a .NET object to inject into a `JsContext` scope. |
| `Extensions` | Static extension methods for JavaScript operations: `Minify`, `TryMinify`, `MinifyAsync`, `RunJavascript`, `RunJavascriptFile`, `JsonFromJsLiteralFile`, and `JsonToDynamic`. |
| `MinifyResult` | Result of a JavaScript minification attempt: `Success`, original `Script`, `MinScript`, and any `Exception`. |
| `ResourceScripts` | Loads embedded `.js` resource files (e.g., `json2.js`) from assembly manifests by namespace path. Caches loaded scripts in a static dictionary. |
| `JsLiteralSchemaManager` | Extends `DaoSchemaManager` to process JavaScript-defined database schemas: parses table definitions, columns, foreign keys, and cross-references from dynamic objects. |
| `JavaScriptSqlProvider` | Abstract base class for SQL execution exposed to JavaScript. Provides `Execute(string sql)` returning `SqlResponse`, with configurable database backend and `IConfigurable` support. |
| `SQLiteJavaScriptSqlProvider` | SQLite implementation of `JavaScriptSqlProvider`. Requires `SQLiteDirectoryPath` and `SQLiteFileName`. |
| `MsSqlJavaScriptSqlProvider` | MS SQL Server implementation. Requires `MsSqlServerName` and `MsSqlDatabaseName`; optionally `MsSqlUserId` and `MsSqlPassword`. |
| `OracleJavaScriptSqlProvider` | Oracle implementation. Requires `OracleUserId`, `OraclePassword`, `OracleServerName`, `OraclePort`, and `OracleInstanceName`. |
| `NpgsqlJavaScriptSqlProvider` | PostgreSQL stub. `Initialize()` throws `NotImplementedException`. |
| `PostgresJavaScriptSqlProvider` | Alias for `NpgsqlJavaScriptSqlProvider` (inherits directly, adds nothing). |
| `SqlResponse` | Result of a SQL query execution: `Count`, `Success`, `Message`, and `Results` array. |
| `NodeScriptRunner` | Runs Node.js scripts as external processes. Auto-detects Node path by OS. |
| `EncryptionAlgorithm` | Enum: `Invalid`, `Aes`, `Rsa`. |

## Dependencies

### Project References
- `bam.base` -- core framework primitives, extension methods (`RandomLetters`, `AddMissing`, etc.)
- `bam.configuration` -- `IConfigurable`, `DefaultConfiguration`
- `bam.data.repositories` -- data repository abstractions
- `bam.data.schema` -- `DaoSchemaManager` for schema generation
- `bam.data` -- `Database`, SQL database implementations

### Package References
None listed explicitly in the `.csproj`, but the code references:
- EcmaScript.NET (via `EcmaScript.NET.Types.Cli` namespace)
- Yahoo.Yui.Compressor (via `Yahoo.Yui.Compressor` namespace)
- Newtonsoft.Json (via `JsonConvert`)

### Embedded Resources
- `json2.js` -- Douglas Crockford's JSON2 library, embedded for use in `JsContext`

## Usage Examples

### Running JavaScript from C#
```csharp
using Bam.Javascript;

var ctx = new JsContext();
ctx.Run("var x = 40 + 2;");
int result = ctx.GetValue<int>("x"); // 42
```

### Exposing .NET objects to JavaScript
```csharp
using Bam.Javascript;

var myService = new MyService();
var ctx = new JsContext();
ctx.SetCliValue("svc", myService);
ctx.Run("svc.DoWork();");
```

### Reading a JS literal schema file
```csharp
using Bam.Javascript;

// Given a file like: var db = { tables: [...], xrefs: [...] };
string json = "path/to/schema.db.js".JsonFromJsLiteralFile("db");
dynamic schema = json.JsonToDynamic();
```

### Minifying JavaScript
```csharp
using Bam.Javascript;

string minified = "function hello() { return 'world'; }".Minify();
```

### Executing SQL through JavaScript context
```csharp
using Bam.Javascript.Sql;

var provider = new SQLiteJavaScriptSqlProvider
{
    SQLiteDirectoryPath = "./data",
    SQLiteFileName = "mydb.sqlite"
};
SqlResponse response = provider.Execute("SELECT * FROM users");
```

## Known Gaps / Not Yet Implemented

- **`NpgsqlJavaScriptSqlProvider.Initialize()`** -- Throws `NotImplementedException`. PostgreSQL support via Npgsql is declared but not functional.
- **`PostgresJavaScriptSqlProvider`** -- Inherits from the unimplemented `NpgsqlJavaScriptSqlProvider` and adds nothing, so it is also non-functional.
- **`Extensions.Minify`** -- Contains a `TODO: use gulp` comment indicating the YUI Compressor-based minification is intended to be replaced with a gulp-based pipeline.
- **`DaoSchema.cs`** -- Excluded from compilation via `<Compile Remove="DaoSchema.cs" />` in the `.csproj`, suggesting it was removed or is in progress.
