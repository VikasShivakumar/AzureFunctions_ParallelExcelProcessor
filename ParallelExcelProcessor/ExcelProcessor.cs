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
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace ParallelExcelProcessor
{
    public class ExcelProcessor
    {
        private readonly IConfiguration configuration;
        public ExcelProcessor(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [FunctionName("ExcelProcessor")]
        public async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger logger)
        {
            logger.LogInformation("Orchestrator started");
            try
            {
                Uri url = new Uri(context.GetInput<string>());

                Stream fileStream = Helpers.GenerateStreamFromBytes(await context.CallActivityAsync<byte[]>("ExcelProcessor_Downloader", url));
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
            catch (Exception ex)
            {
                logger.LogError(ex, "Orchestrator Failed");
            }
            logger.LogInformation("Orchestrator ended");
        }

        [FunctionName("ExcelProcessor_Downloader")]
        public async Task<byte[]> DownloadFileStreamAsync([ActivityTrigger]Uri url, ILogger logger)
        {
            CloudStorageAccount storageAccount = new AzureStorageConnectionFactory().GetCloudStorageAccount(configuration);
            ICloudBlob Blob = await storageAccount.CreateCloudBlobClient().GetBlobReferenceFromServerAsync(url);
            byte[] fileData = new byte[Blob.Properties.Length];
            await Blob.DownloadToByteArrayAsync(fileData,0);
            logger.LogInformation("File downloaded");
            return fileData;
        }

        [FunctionName("ExcelProcessor_Processor")]
        public async Task<bool> ProcessDataTable([ActivityTrigger] DataTable input, ILogger logger)
        {
            try
            {
                logger.LogInformation("Activity started");
                await Helpers.WriteToSQL(input, configuration);
                logger.LogInformation("Activity ended");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error Occurred in activity");
                return false;
            }
        }


        [FunctionName("ExcelProcessor_HttpStart")]
        public async Task<HttpResponseMessage> HttpStart(
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