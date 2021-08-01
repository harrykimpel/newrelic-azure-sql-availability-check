using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Text;
using System.Net.Http.Headers;

public static async Task Run(TimerInfo myTimer, ILogger log)
{
    log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

    var str = Environment.GetEnvironmentVariable("SQLDB_CONNECTION");
    log.LogInformation($"conn str: {str}");
            
    using (SqlConnection conn = new SqlConnection(str))
    {
        conn.Open();
        //var text = "UPDATE SalesLT.SalesOrderHeader " +
        //        "SET [Status] = 5  WHERE ShipDate < GetDate();";
        var text = Environment.GetEnvironmentVariable("SQLDB_QUERY");
        
        using (SqlCommand cmd = new SqlCommand(text, conn))
        {
            // Execute the command and log the # rows affected.
            //var rows = await cmd.ExecuteNonQueryAsync();
            DateTime start = new DateTime();
            var reader = await cmd.ExecuteReaderAsync();
            //log.LogInformation($"{rows} rows were updated");
            int counter = 0;  
            while (reader.Read())
            {
                counter++;
            }
            DateTime end = new DateTime();
            TimeSpan interval = end - start;
            log.LogInformation($"{counter} rows read");
            log.LogInformation($"{interval.TotalMilliseconds} interval");

            HttpClient _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://insights-collector.newrelic.com");
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-Insert-Key", Environment.GetEnvironmentVariable("NR_INSERT_KEY"));
        
            string telemetry = "{ \"eventType\": \"AzureSQLManaged\", \"RowsRead\": \""+counter+"\", \"TotalMillisecondsExecuteReader\": "+interval.TotalMilliseconds+", \"TicksExecuteReader\": "+interval.Ticks+" }";
            HttpContent httpContent = new StringContent(telemetry, Encoding.UTF8);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            
            string nrAccount = Environment.GetEnvironmentVariable("NR_ACCOUNT");
            string ingestPath = "/v1/accounts/"+nrAccount+"/events";
            var response = await _httpClient.PostAsync(ingestPath, httpContent);
            bool isSuccess = false;
            if (response.IsSuccessStatusCode)
            {
                isSuccess = true;
            }
            log.LogInformation($"Success? {isSuccess}");
            string resp = await response.Content.ReadAsStringAsync();
            log.LogInformation($"resp: {resp}");
        }
    }
}
