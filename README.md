# OracleClientTest

This project demonstrates and benchmarks various data access operations using Oracle Data Provider for .NET (ODP.NET) Managed Driver. It includes functionalities for bulk data insertion, data selection, and a long-running connection test against an Oracle Database.

## Features

-   **Bulk Data Insertion**: Implements and benchmarks several methods for bulk inserting data into an Oracle table, including `OracleBulkCopy` and array binding techniques.
-   **Data Selection**: Demonstrates different approaches to selecting data, including using `WHERE IN` clauses with literal values and bind variables (Associative Arrays).
-   **Long Connection Test**: Simulates multiple concurrent threads maintaining database connections over an extended period to test connection stability and pooling.
-   **Benchmarking**: Measures and reports performance statistics (average time, max time, table size) for each data operation.

## Prerequisites

-   .NET 8.0 SDK or later
-   Access to an Oracle Database instance.

## Configuration

Configuration for the application is managed through an `appsettings.json` file. A template file, `appsettings.template.json`, is provided in the project.

1.  **Create your configuration file**: Make a copy of `appsettings.template.json` and rename it to `appsettings.json`.
2.  **Edit `appsettings.json`**: Open the `appsettings.json` file and modify the values to match your environment.

### Database Connection
Update the `Database` section with your Oracle instance details:
```json
{
  "Database": {
    "ServiceName": "your_service_name",
    "Hostname": "your_hostname",
    "Port": "1521",
    "Username": "your_username",
    "Password": "your_password"
  },
  // ... other settings
}
```

### Benchmark and Other Settings
You can also customize benchmark parameters, pooling settings, and long connection test variables in this file.

### Pooling Settings
The following parameters control the behavior of the ODP.NET connection pool. The default values for the application are set in `appsettings.json`, which may differ from the driver's default.

-   **`Enabled` (`Pooling`)**: Enables or disables connection pooling.
    -   App Default: `true`
    -   Driver Default: `true`
-   **`MinPoolSize` (`Min Pool Size`)**: The minimum number of connections in the pool.
    -   App Default: `100`
    -   Driver Default: `1`
-   **`MaxPoolSize` (`Max Pool Size`)**: The maximum number of connections allowed in the pool.
    -   App Default: `100`
    -   Driver Default: `100`
-   **`IncrPoolSize` (`Incr Pool Size`)**: The number of new connections to be created when all connections in the pool are in use.
    -   App Default: `1`
    -   Driver Default: `5`
-   **`DecrPoolSize` (`Decr Pool Size`)**: The number of connections that are closed when an excessive amount of established connections are unused.
    -   App Default: `1`
    -   Driver Default: `1`
-   **`ConnectionTimeout` (`Connection Timeout`)**: The time to wait (in seconds) for an idle connection from the pool before a timeout error occurs.
    -   App Default: `15`
    -   Driver Default: `15`
-   **`ValidateConnection` (`Validate Connection`)**: Enables validation of connections coming from the pool.
    -   App Default: `false`
    -   Driver Default: `false`
-   **`PromotableTransaction`**: Specifies whether the transaction can be promoted to a distributed transaction. This attribute is not part of the standard ODP.NET connection string but is used by the application.
    -   App Default: `"promotable"`
-   **`Enlist`**: Specifies whether the connection will be enlisted in the thread's current transaction context.
    -   App Default: `true`
    -   Driver Default: `true`

For more details, refer to the official [Oracle Data Provider for .NET Developer's Guide](https://docs.oracle.com/en/database/oracle/oracle-data-access-components/19.3/odpnt/featConnecting.html#GUID-BCF2F215-C25F-403C-8D18-B03A69BC7104).

## How to Run

1.  **Navigate to the project directory**:
    ```bash
    cd OracleClientTest
    ```
2.  **Restore NuGet packages** (if not already restored):
    ```bash
    dotnet restore
    ```
3.  **Run the application**:
    ```bash
    dotnet run
    ```

You can also pass `rowCount` and `loopCount` as arguments to `dotnet run`. For example, to run with 1000 rows and 5 loops:

```bash
dotnet run 1000 5
```
