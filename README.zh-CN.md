# OracleClientTest

本专案旨在使用 Oracle Data Provider for .NET (ODP.NET) Managed Driver 展示并评测各种资料存取操作。它包含了对 Oracle 资料库进行大量资料插入、资料查询以及长时间连线测试的功能。

## 功能

-   **大量资料插入**: 实作并评测多种将大量资料插入 Oracle 表的方法，包含 `OracleBulkCopy` 和阵列系结技术。
-   **资料查询**: 展示不同的资料查询方法，包含使用 `WHERE IN` 子句搭配常值和系结变数 (关联阵列)。
-   **长时间连线测试**: 模拟多个并行执行绪长时间维持资料库连线，以测试连线稳定性和集区功能。
-   **效能评测**: 为每个资料操作测量并回报效能统计资料 (平均时间、最大时间、资料表大小)。

## 先决条件

-   .NET 8.0 SDK 或更新版本
-   可存取的 Oracle 资料库执行个体

## 组态设定

本应用程式的组态是透过 `appsettings.json` 檔案进行管理。专案中已提供一个范本檔案 `appsettings.template.json`。

1.  **建立您的组态檔**：复制 `appsettings.template.json` 并将其重新命名为 `appsettings.json`。
2.  **编辑 `appsettings.json`**：开启 `appsettings.json` 檔案，并修改其中的值以符合您的环境。

### 资料库连线
更新 `Database` 区段，填入您的 Oracle 执行个体详细资讯：
```json
{
  "Database": {
    "ServiceName": "your_service_name",
    "Hostname": "your_hostname",
    "Port": "1521",
    "Username": "your_username",
    "Password": "your_password"
  },
  // ... 其他设定
}
```

### 评测與其他设定
您也可以在此檔案中自订评测參數、连线池设定和长时间连线测试的变数。

## 如何执行

1.  **切换至专案目录**：
    ```bash
    cd OracleClientTest
    ```
2.  **还原 NuGet 套件** (若尚未还原)：
    ```bash
    dotnet restore
    ```
3.  **执行应用程式**：
    ```bash
    dotnet run
    ```

您也可以将 `rowCount` 和 `loopCount` 作为參數传递给 `dotnet run`。例如，若要以 1000 笔资料和 5 次迴圈执行：

```bash
dotnet run 1000 5
```
