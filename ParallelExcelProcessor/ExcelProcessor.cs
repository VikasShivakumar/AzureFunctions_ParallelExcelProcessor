using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ParallelExcelProcessor
{
    public static class ExcelProcessor
    {
        [FunctionName("ExcelProcessor")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger logger)
        {
            logger.LogInformation("Orchestrator started");
            
            //HttpClient httpClient = new HttpClient();
            //using (Stream fileStream = await httpClient.GetStreamAsync(context.GetInput<string>()))

            using (Stream fileStream = File.OpenRead(context.GetInput<string>()))
            {

                IEnumerable<DataTable> data = new ExcelParser().Parse(fileStream);
                List<Task<bool>> parallelTasks = new List<Task<bool>>();

                foreach (DataTable table in data)
                {
                    Task<bool> task = context.CallActivityAsync<bool>("ExcelProcessor_Processor", table);
                    parallelTasks.Add(task);
                }

                await Task.WhenAll(parallelTasks);

                foreach (var task in parallelTasks)
                {
                    logger.LogInformation(await task ? "true" : "false");
                }
            }
            logger.LogInformation("Orchestrator ended");
        }

        [FunctionName("ExcelProcessor_Processor")]
        public static async Task<bool> ProcessDataTable([ActivityTrigger] DataTable dataTable, IConfiguration configuration, ILogger logger)
        {
            logger.LogInformation("Activity started");
            await Helpers.WriteToSQL(dataTable, configuration);
            logger.LogInformation("Activity ended");
            return true;
        }


        [FunctionName("ExcelProcessor_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger logger)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            string requestBody = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string url = data?.FileLocation;
            if (string.IsNullOrEmpty(url))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
            else
            {
                // Function input comes from the request content.
                string instanceId = await starter.StartNewAsync(orchestratorFunctionName: "ExcelProcessor", input: url);

                logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");

                return starter.CreateCheckStatusResponse(req, instanceId);
            }

        }
    }
}