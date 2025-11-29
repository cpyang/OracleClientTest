# OracleClientTest

本專案旨在使用 Oracle Data Provider for .NET (ODP.NET) Managed Driver 展示並評測各種資料存取操作。它包含了對 Oracle 資料庫進行大量資料插入、資料查詢以及長時間連線測試的功能。

## 功能

-   **大量資料插入**: 實作並評測多種將大量資料插入 Oracle 資料表的方法，包含 `OracleBulkCopy` 和陣列繫結技術。
-   **資料查詢**: 展示不同的資料查詢方法，包含使用 `WHERE IN` 子句搭配常值和繫結變數 (關聯陣列)。
-   **長時間連線測試**: 模擬多個並行執行緒長時間維持資料庫連線，以測試連線穩定性和集區功能。
-   **效能評測**: 為每個資料操作測量並回報效能統計資料 (平均時間、最大時間、資料表大小)。

## 先決條件

-   .NET 8.0 SDK 或更新版本
-   可存取的 Oracle 資料庫執行個體

## 組態設定

本應用程式的組態是透過 `appsettings.json` 檔案進行管理。專案中已提供一個範本檔案 `appsettings.template.json`。

1.  **建立您的組態檔**：複製 `appsettings.template.json` 並將其重新命名為 `appsettings.json`。
2.  **編輯 `appsettings.json`**：開啟 `appsettings.json` 檔案，並修改其中的值以符合您的環境。

### 資料庫連線
更新 `Database` 區段，填入您的 Oracle 執行個體詳細資訊：
```json
{
  "Database": {
    "ServiceName": "your_service_name",
    "Hostname": "your_hostname",
    "Port": "1521",
    "Username": "your_username",
    "Password": "your_password"
  },
  // ... 其他設定
}
```

### 評測與其他設定
您也可以在此檔案中自訂評測參數、連線池設定和長時間連線測試的變數。

## 如何執行

1.  **切換至專案目錄**：
    ```bash
    cd OracleClientTest
    ```
2.  **還原 NuGet 套件** (若尚未還原)：
    ```bash
    dotnet restore
    ```
3.  **執行應用程式**：
    ```bash
    dotnet run
    ```

您也可以將 `rowCount` 和 `loopCount` 作為參數傳遞給 `dotnet run`。例如，若要以 1000 筆資料和 5 次迴圈執行：

```bash
dotnet run 1000 5
```
