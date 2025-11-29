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
