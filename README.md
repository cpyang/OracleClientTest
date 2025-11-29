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

Before running the application, you must configure your Oracle Database connection details. Open the `OracleClientTest.cs` file and locate the `Main` method. Update the following variables with your database credentials and connection information:

```csharp
            string db_name = "your_service_name"; // e.g., "ORCLPDB1" or "XE"
            string hostname = "your_hostname";     // e.g., "localhost"
            string port = "your_port";             // e.g., "1521"
            string username = "your_username";     // e.g., "scott"
            string password = "your_password";     // e.g., "tiger"
```

## How to Run

1.  **Navigate to the project directory**:
    ```bash
    cd /home/cpyang/src/ODP.Net/OracleClientTest
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
