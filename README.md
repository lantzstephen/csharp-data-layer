# C# Data Access Layer

A lightweight .NET Core data access layer demonstrating enterprise patterns for SQL Server database operations.

## Overview

This is a sanitized sample from production code I wrote at a financial services firm. It demonstrates:

- **Parameterized queries** - SQL injection prevention
- **IDisposable patterns** - Proper resource cleanup with `using` statements
- **Dependency injection** - Configuration passed via `IConfiguration` rather than instantiated
- **Active Directory integration** - LDAP queries for role-based authorization
- **Multiple return patterns** - DataTable, DataSet, JSON serialization

## Files

### DataAccess.cs
Core database operations:
- `ExecSQL()` - Execute queries returning DataTable
- `ExecSQLNonQuery()` - Execute INSERT/UPDATE/DELETE
- `ExecSQLMulti()` - Execute queries returning multiple result sets
- `ExecJson()` - Execute queries with SQL Server FOR JSON AUTO
- `LookupString()` / `LookupInt()` - Single-value lookups

### Security.cs
Active Directory integration:
- `Role` enum - Authorization levels
- `UserIsInADGroup()` - Check AD group membership using tokenGroups
- `ADGroupsGranted()` - Get all matching AD groups for a user

## Key Patterns Demonstrated

### Dependency Injection
```csharp
public static DataTable ExecSQL(
    IConfiguration configuration,  // Injected, not instantiated
    string cmdText,
    ...)
```

### IDisposable / Resource Cleanup
```csharp
using (SqlConnection conn = new SqlConnection(connection))
using (SqlCommand cmd = new SqlCommand())
{
    // Connection automatically closed even on exception
}
```

### Parameterized Queries
```csharp
SqlParameter[] parameters = new SqlParameter[2];
parameters[0] = new SqlParameter("@Name", value);
cmd.Parameters.AddRange(parameters);
```

## Technology Stack

- .NET Core 2.2+
- System.Data.SqlClient
- System.DirectoryServices
- Newtonsoft.Json
- Microsoft.Extensions.Configuration

## Author

Stephen Lantz - Senior Database Engineer
20+ years SQL Server, PostgreSQL, ETL, and data architecture

## Note

This is representative sample code, sanitized from proprietary systems. It demonstrates coding patterns and style rather than a complete runnable application.
