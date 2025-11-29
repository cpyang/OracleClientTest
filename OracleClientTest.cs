using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
// https://www.nuget.org/packages/Oracle.ManagedDataAccess/12.1.24230118#readme-body-tab
// dotnet add package Oracle.ManagedDataAccess --version 12.1.24230118
// dotnet add package Oracle.ManagedDataAccess --version 19.3.0

namespace OracleClientTest
{
    class OracleClientTest
    {
        public static void Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Database settings
            string db_name = config.GetValue<string>("Database:ServiceName");
            string hostname = config.GetValue<string>("Database:Hostname");
            string port = config.GetValue<string>("Database:Port");
            string username = config.GetValue<string>("Database:Username");
            string password = config.GetValue<string>("Database:Password");

            // Benchmark settings
            long rowCount = config.GetValue<long>("Benchmark:RowCount");
            int loopCount = config.GetValue<int>("Benchmark:LoopCount");
            string table_name = config.GetValue<string>("Benchmark:TableName");
            string tableDDL = config.GetValue<string>("Benchmark:TableDDL");

            if (args.Length == 2)
            {
                rowCount = long.Parse(args[0]);
                loopCount = int.Parse(args[1]);
            }

            Console.Write("Oracle");
            Console.Write("(" + rowCount + " Rows x " + loopCount + ")");
            Console.WriteLine();

            // Pooling settings
            string pooling = config.GetValue<bool>("Pooling:Enabled").ToString().ToLower();
            int minPool = config.GetValue<int>("Pooling:MinPoolSize");
            int maxPool = config.GetValue<int>("Pooling:MaxPoolSize");
            int incrPool = config.GetValue<int>("Pooling:IncrPoolSize");
            int decrPool = config.GetValue<int>("Pooling:DecrPoolSize");
            int connectTimeOut = config.GetValue<int>("Pooling:ConnectionTimeout");
            string validate = config.GetValue<bool>("Pooling:ValidateConnection").ToString().ToLower();
            string promotableTransaction = config.GetValue<string>("Pooling:PromotableTransaction");
            string enlist = config.GetValue<bool>("Pooling:Enlist").ToString().ToLower();

            string poolStr =
                    "Pooling=" + pooling + "; " +
                    "Min Pool Size=" + minPool + "; " + "Max Pool Size=" + maxPool + "; " +
                    "Incr Pool Size=" + incrPool + "; " + "Decr Pool Size=" + decrPool + "; " +
                    "Self Tuning=true; " +
                    "Promotable Transaction=" + promotableTransaction + "; " + "Enlist=" + enlist + "; " +
                    "Connection Timeout=" + connectTimeOut + "; " + "Validate Connection=" + validate + ";";

            string orclconnstr =
                "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=" + hostname + ")(PORT=" + port + "))(CONNECT_DATA=(SERVICE_NAME=" + db_name + ")));User Id=" + username + ";Password=" + password + ";" + poolStr;

            var bulk = new BulkInsertData(orclconnstr, config);
            bulk.dropTable(table_name);
            bulk.createTable(tableDDL);
            DataTable dt = bulk.getSchemaInfo(table_name);
            DataRow row;
            for (long i = 0; i < rowCount; i++)
            {
                row = dt.NewRow();
                row["ID"] = i;
                row["VALUE"] = string.Concat(Enumerable.Repeat("X", 4000));
                dt.Rows.Add(row);
            }

            // Prepare data for Select tests
            string whereCondition = "ID = 0";
            var idsToSelect = new List<int> { 1, 3, 5 };

            // Define benchmarks
            var benchmarks = new List<(string Name, Action Action)>
            {
                ("BulkInsert", () => bulk.bulkInsert(table_name, dt)),
                ("BulkInsert2", () => bulk.bulkInsert2(table_name, dt)),
                ("BulkInsert3", () => bulk.bulkInsert3(table_name, dt)),
                ("BulkCopy", () => bulk.bulkCopy(table_name, dt)),
                ("ProcInsert", () => bulk.procInsert(table_name, dt)),
                ("SelectData", () => bulk.selectData(table_name, whereCondition)),
                ("SelectWithBind", () => bulk.selectDataWithBind(table_name, idsToSelect)),
                ("SelectWithLiteral", () => bulk.selectDataWithLiteral(table_name, idsToSelect))
            };

            // Special setup for ProcInsert
            bulk.createProcedure();

            // Execute benchmarks
            foreach (var benchmark in benchmarks)
            {
                RunBenchmark(benchmark.Name, loopCount, benchmark.Action, () => bulk.getTableSize(table_name));
            }

            // Long Connection Test
            bulk.testLongConnection();

            // Close Connection
            bulk.closeConnection();
        }

        public static void RunBenchmark(string name, int loopCount, Action action, Func<long> getTableSize)
        {
            var times = new List<long>();
            for (int i = 0; i < loopCount; i++)
            {
                var watch = Stopwatch.StartNew();
                action();
                watch.Stop();
                times.Add(watch.ElapsedMilliseconds);
            }
            PrintStatistics(name, times, getTableSize());
        }

        public static void PrintStatistics(string operationName, List<long> elapsedTimes, long tableSize)
        {
            if (elapsedTimes.Count == 0)
            {
                Console.WriteLine($"{operationName}\tNo data");
                return;
            }

            double average = elapsedTimes.Average();
            long max = elapsedTimes.Max();

            Console.WriteLine($"{operationName}\tAverage={average:F0} ms\tMax={max} ms\tTableSize={tableSize / 1048576} MB");

            // Histogram
            var histogram = new SortedDictionary<long, int>();
            long bucketSize = 10;

            foreach (var time in elapsedTimes)
            {
                long bucket = (time / bucketSize) * bucketSize;
                if (!histogram.ContainsKey(bucket))
                {
                    histogram[bucket] = 0;
                }
                histogram[bucket]++;
            }

            Console.WriteLine("  Histogram (ms):");
            foreach (var entry in histogram)
            {
                Console.WriteLine($"    {entry.Key:D3}-{entry.Key + bucketSize - 1:D3}\t: {new string('*', entry.Value)}");
            }
        }
    }

    class BulkInsertData
    {
        private readonly string orclconnstr;
        private readonly IConfiguration config;
        public OracleConnection conn;

        public BulkInsertData(string connectionString, IConfiguration configuration)
        {
            this.orclconnstr = connectionString;
            this.config = configuration;
            this.conn = new OracleConnection(orclconnstr);
            this.conn.KeepAlive = false;
            Console.WriteLine("KeepAlive = " + this.conn.KeepAlive);
        }

        public void closeConnection()
        {
            this.conn.Close();
            this.conn.Dispose();
        }

        public void createTable(String tableDDL)
        {
            OracleCommand cmd = null;
            using (OracleConnection conn = new OracleConnection(orclconnstr))
            {
                try
                {
                    conn.Open();
                    cmd = conn.CreateCommand();
                    cmd.CommandText = tableDDL;
                    cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message + ":\n" + e.StackTrace);
                }
            }
        }

        public void dropTable(String table_name)
        {
            OracleCommand cmd = null;
            using (OracleConnection conn = new OracleConnection(orclconnstr))
            {
                string tableDDL = "DROP TABLE " + table_name;
                try
                {
                    conn.Open();
                    cmd = conn.CreateCommand();
                    cmd.CommandText = tableDDL;
                    cmd.ExecuteNonQuery();
                }
                catch (Exception)
                {
                    // Ignore exceptions on drop
                }
            }
        }

        public void createProcedure()
        {
            string tableName = config.GetValue<string>("Benchmark:TableName");
            ExecuteSQL("create or replace procedure insert_data(p1 IN NUMBER, p2 IN VARCHAR2)" +
                        "IS " +
                        "BEGIN " +
                          $" INSERT INTO {tableName} VALUES(p1,p2); " +
                        "END;");
        }

        public void ExecuteSQL(string sqlText)
        {
            using (OracleConnection conn = new OracleConnection(orclconnstr))
            {
                conn.Open();
                OracleCommand cmd = new OracleCommand(sqlText, conn);
                cmd.ExecuteNonQuery();
                cmd.Dispose();
            }
        }

        public long getTableSize(String table_name)
        {
            long segment_size = 0;
            using (OracleConnection conn = new OracleConnection(orclconnstr))
            {
                try
                {
                    conn.Open();
                    var cmd = new OracleCommand("select sum(bytes) as bytes from user_segments " +
                            "where segment_type='TABLE' and segment_name=upper(:TABLE_NAME) group by segment_name",
                            conn);
                    cmd.BindByName = true;
                    OracleParameter pTableName =
                        new OracleParameter("TABLE_NAME", OracleDbType.Varchar2);
                    pTableName.Value = table_name;
                    cmd.Parameters.Add(pTableName);

                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            segment_size = reader.GetInt64(0);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message + ":\n" + e.StackTrace);
                }
            }
            return segment_size;
        }

        public DataTable getSchemaInfo(string table_name)
        {
            DataTable typeMappingTable = new DataTable();
            using (OracleConnection conn = new OracleConnection(orclconnstr))
            {
                try
                {
                    conn.Open();
                    var cmd = new OracleCommand("select * from " + table_name, conn);
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        DataTable schemaTable = reader.GetSchemaTable();
                        foreach (DataRow row in schemaTable.Rows)
                        {
                            var fNameColumn = new DataColumn();
                            if (row["DATATYPE"] != null)
                            {
                                var typeName = row["DATATYPE"].ToString();
                                if (typeName != null)
                                {
                                    var type = Type.GetType(typeName);
                                    if (type != null)
                                    {
                                        fNameColumn.DataType = type;
                                    }
                                }
                            }
                            fNameColumn.ColumnName = row["COLUMNNAME"].ToString();
                            typeMappingTable.Columns.Add(fNameColumn);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message + ":\n" + e.StackTrace);
                }
            }
            return typeMappingTable;
        }

        public void bulkCopy(string table_name, DataTable table)
        {
            using (OracleConnection conn = new OracleConnection(orclconnstr))
            {
                try
                {
                    conn.Open();
                    using (var bulkCopy = new OracleBulkCopy(conn, OracleBulkCopyOptions.UseInternalTransaction))
                    {
                        bulkCopy.BulkCopyTimeout = 0;
                        bulkCopy.DestinationTableName = table_name;
                        bulkCopy.WriteToServer(table);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public void bulkInsert(string table_name, DataTable table)
        {
            using (OracleConnection conn = new OracleConnection(orclconnstr))
            {
                try
                {
                    conn.Open();
                    using (OracleTransaction tr = conn.BeginTransaction())
                    {
                        OracleCommand command = conn.CreateCommand();
                        command.CommandText = $"INSERT INTO {table_name} (ID, VALUE) VALUES (:ID, :VALUE)";
                        command.ArrayBindCount = table.Rows.Count;
                        command.BindByName = true;

                        foreach (DataColumn col in table.Columns)
                        {
                            switch (col.DataType.ToString())
                            {
                                case "System.Int32":
                                    command.Parameters.Add(col.ColumnName, OracleDbType.Int32,
                                        table.AsEnumerable().Select(c => c.Field<int>(col.ColumnName)).ToArray(),
                                        ParameterDirection.Input);
                                    break;
                                case "System.Int64":
                                case "System.Decimal":
                                    var param = new OracleParameter(col.ColumnName, OracleDbType.Decimal);
                                    decimal[] values = new decimal[table.Rows.Count];
                                    for (int i = 0; i < table.Rows.Count; i++)
                                    {
                                        values[i] = Convert.ToDecimal(table.Rows[i][col.ColumnName]);
                                    }
                                    param.Value = values;
                                    command.Parameters.Add(param);
                                    break;
                                case "System.String":
                                    command.Parameters.Add(col.ColumnName, OracleDbType.Varchar2,
                                        table.AsEnumerable().Select(c => c.Field<string>(col.ColumnName)).ToArray(),
                                        ParameterDirection.Input);
                                    break;
                                default:
                                    Console.WriteLine("XXX Unhandled: " + col.DataType);
                                    break;
                            }
                        }
                        command.ExecuteNonQuery();
                        tr.Commit();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public void bulkInsert2(string table_name, DataTable table)
        {
            using (OracleConnection conn = new OracleConnection(orclconnstr))
            {
                try
                {
                    conn.Open();
                    using (OracleTransaction tr = conn.BeginTransaction())
                    {
                        var pID = new OracleParameter("ID", OracleDbType.Decimal);
                        var pVALUE = new OracleParameter("VALUE", OracleDbType.Varchar2);

                        decimal[] ids = new decimal[table.Rows.Count];
                        string[] values = new string[table.Rows.Count];
                        for (int i = 0; i < table.Rows.Count; i++)
                        {
                            ids[i] = Convert.ToDecimal(table.Rows[i]["ID"]);
                            values[i] = Convert.ToString(table.Rows[i]["VALUE"]);
                        }
                        pID.Value = ids;
                        pVALUE.Value = values;

                        OracleCommand command = conn.CreateCommand();
                        command.CommandText = $"INSERT INTO {table_name} (ID, VALUE) VALUES (:ID, :VALUE)";
                        command.ArrayBindCount = ids.Length;
                        command.BindByName = true;
                        command.Parameters.Add(pID);
                        command.Parameters.Add(pVALUE);
                        command.ExecuteNonQuery();
                        tr.Commit();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public void bulkInsert3(string table_name, DataTable table)
        {
            using (OracleConnection conn = new OracleConnection(orclconnstr))
            {
                try
                {
                    conn.Open();
                    using (OracleTransaction tr = conn.BeginTransaction())
                    {
                        var pID = new OracleParameter("ID", OracleDbType.Int64);
                        var pVALUE =
                            new OracleParameter("VALUE", OracleDbType.Varchar2);

                        long[] ids = new long[table.Rows.Count];
                        string[] values = new string[table.Rows.Count];
                        for (int i = 0; i < table.Rows.Count; i++)
                        {
                            ids[i] = Convert.ToInt64(table.Rows[i]["ID"]);
                            values[i] = Convert.ToString(table.Rows[i]["VALUE"]);
                        }
                        pID.Value = ids;
                        pVALUE.Value = values;

                        OracleCommand command = conn.CreateCommand();
                        command.CommandText =
                            $"INSERT INTO {table_name} (ID, VALUE) VALUES (:ID, :VALUE)";
                        command.ArrayBindCount = ids.Length;
                        command.BindByName = true;
                        command.Parameters.Add(pID);
                        command.Parameters.Add(pVALUE);
                        command.ExecuteNonQuery();
                        tr.Commit();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public void procInsert(string table_name, DataTable table)
        {
            using (OracleConnection conn = new OracleConnection(orclconnstr))
            {
                try
                {
                    conn.Open();
                    using (OracleTransaction tr = conn.BeginTransaction())
                    {
                        OracleCommand command = conn.CreateCommand();
                        command.CommandText = "INSERT_DATA";
                        command.CommandType = CommandType.StoredProcedure;

                        foreach (DataRow row in table.Rows)
                        {
                            command.Parameters.Clear(); // Clear parameters for each iteration
                            command.Parameters.Add("p1", OracleDbType.Decimal).Value = Convert.ToDecimal(row["ID"]);
                            command.Parameters.Add("p2", OracleDbType.Varchar2).Value = Convert.ToString(row["VALUE"]);
                            command.ExecuteNonQuery();
                        }
                        tr.Commit();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.ToString());
                }
            }
        }
        public void selectData(string table_name, string where_condition)
        {
            using (OracleConnection conn = new OracleConnection(orclconnstr))
            {
                try
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = $"SELECT ID, VALUE FROM {table_name} WHERE {where_condition}";
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            long id = reader.GetInt64(0);
                            string value = reader.GetString(1);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message + ":\n" + e.StackTrace);
                }
            }
        }

        public void selectDataWithLiteral(string table_name, List<int> ids)
        {
            using (OracleConnection conn = new OracleConnection(orclconnstr))
            {
                try
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    string idList = string.Join(",", ids);
                    cmd.CommandText = $"SELECT ID, VALUE FROM {table_name} WHERE ID IN ({idList})";
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            long id = reader.GetInt64(0);
                            string value = reader.GetString(1);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message + ":\n" + e.StackTrace);
                }
            }
        }

        public void selectDataWithBind(string table_name, List<int> ids)
        {
            using (OracleConnection conn = new OracleConnection(orclconnstr))
            {
                try
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();

                    cmd.CommandText = $@"
                        DECLARE
                            TYPE t_num_list IS TABLE OF NUMBER INDEX BY PLS_INTEGER;
                            v_in_ids t_num_list;
                            v_ids SYS.ODCINUMBERLIST := SYS.ODCINUMBERLIST();
                            v_idx PLS_INTEGER;
                        BEGIN
                            v_in_ids := :p_id_list;
                            
                            v_idx := v_in_ids.FIRST;
                            WHILE v_idx IS NOT NULL LOOP
                                v_ids.EXTEND;
                                v_ids(v_ids.LAST) := v_in_ids(v_idx);
                                v_idx := v_in_ids.NEXT(v_idx);
                            END LOOP;

                            OPEN :rc FOR
                            SELECT ID, VALUE FROM {table_name}
                            WHERE ID IN (SELECT COLUMN_VALUE FROM TABLE(v_ids));
                        END;";
                    cmd.BindByName = true;

                    OracleParameter p = new OracleParameter();
                    p.ParameterName = "p_id_list";
                    p.OracleDbType = OracleDbType.Int32;
                    p.CollectionType = OracleCollectionType.PLSQLAssociativeArray;
                    p.Value = ids.ToArray();
                    p.Size = ids.Count;
                    cmd.Parameters.Add(p);

                    cmd.Parameters.Add("rc", OracleDbType.RefCursor, ParameterDirection.Output);

                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            long id = reader.GetInt64(0);
                            string value = reader.GetString(1);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message + ":\n" + e.StackTrace);
                }
            }
        }

        public void testLongConnection()
        {
            int numThreads = config.GetValue<int>("LongConnectionTest:NumThreads");
            int runDays = config.GetValue<int>("LongConnectionTest:RunDays");

            Console.WriteLine($"Starting Long Connection Test for {runDays} days with {numThreads} concurrent threads.");

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromDays(runDays));
            var cancellationToken = cancellationTokenSource.Token;
            var tasks = new List<Task>();
            var random = new Random();

            Action<object> threadAction = (threadIdObj) =>
            {
                int threadId = (int)threadIdObj;
                using (OracleConnection conn = new OracleConnection(this.orclconnstr))
                {
                    string sql = "SELECT SID, SERIAL# FROM V$SESSION WHERE AUDSID = SYS_CONTEXT('USERENV', 'SESSIONID')";
                    int round = 0;
                    try
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            round++;
                            Console.WriteLine($"[Thread {threadId}] Round {round}: Opening Connection...");
                            conn.Open();
                            using (OracleCommand cmd = new OracleCommand(sql, conn))
                            {
                                using (OracleDataReader reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        int sid = reader.GetInt32(0);
                                        int serial = reader.GetInt32(1);
                                        Console.WriteLine($"[Thread {threadId}] Round {round}: Session ID = {sid},{serial}");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"[Thread {threadId}] Round {round}: Could not retrieve session information.");
                                    }
                                }

                                int waitSeconds = random.Next(1, 11);
                                Console.WriteLine($"[Thread {threadId}] Round {round}: Waiting for {waitSeconds} seconds...");

                                try
                                {
                                    Task.Delay(TimeSpan.FromSeconds(waitSeconds), cancellationToken).Wait();
                                }
                                catch (OperationCanceledException)
                                {
                                    Console.WriteLine($"[Thread {threadId}] Round {round}: Sleep cancelled. Exiting loop.");
                                    break;
                                }

                                if (!cancellationToken.IsCancellationRequested)
                                {
                                    using (OracleDataReader reader = cmd.ExecuteReader())
                                    {
                                        if (reader.Read())
                                        {
                                            int sid = reader.GetInt32(0);
                                            int serial = reader.GetInt32(1);
                                            Console.WriteLine($"[Thread {threadId}] Round {round}: (After wait) Session ID = {sid},{serial}");
                                        }
                                        else
                                        {
                                            Console.WriteLine($"[Thread {threadId}] Round {round}: (After wait) Could not retrieve session information.");
                                        }
                                    }
                                }
                            }
                            Console.WriteLine($"[Thread {threadId}] Round {round}: Connection State = {conn.State}");
                            Console.WriteLine($"[Thread {threadId}] Round {round}: Closing Connection...");
                            conn.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        if (!(e is OperationCanceledException && cancellationToken.IsCancellationRequested))
                        {
                            Console.WriteLine($"[Thread {threadId}] Error: {e.Message}\n{e.StackTrace}");
                        }
                    }
                    finally
                    {
                         if (conn.State == ConnectionState.Open)
                         {
                             conn.Close();
                         }
                         Console.WriteLine($"[Thread {threadId}] has finished.");
                    }
                }
            };

            for (int i = 0; i < numThreads; i++)
            {
                int threadId = i;
                Task t = Task.Factory.StartNew(() => threadAction(threadId), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                tasks.Add(t);
                Console.WriteLine($"Started task {threadId}");
            }

            Console.WriteLine($"All tasks started. Test will run for {runDays} days. Main thread is waiting for completion...");

            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (AggregateException ae)
            {
                Console.WriteLine("One or more tasks threw an exception:");
                foreach (var ex in ae.InnerExceptions)
                {
                     if (!(ex is OperationCanceledException))
                     {
                        Console.WriteLine($"   {ex.GetType().Name}: {ex.Message}");
                     }
                }
            }


            Console.WriteLine("All tasks have completed.");
        }
    }
}

/* vim: set tabstop=4:softtabstop=4:shiftwidth=4:expandtab */

