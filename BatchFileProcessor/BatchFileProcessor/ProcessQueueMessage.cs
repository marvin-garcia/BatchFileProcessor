using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace BatchFileProcessor
{
    public static class ProcessQueueMessage
    {
        private static string _connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        private static string _containerName = Environment.GetEnvironmentVariable("ContainerName");

        [FunctionName("ProcessQueueMessage")]
        public static async Task Run(
            [QueueTrigger("%QueueName%")] string queueItem,
            //[BlobTrigger("batchfiles/{name}", Connection = "AzureWebJobsStorage")] Stream blob,
            [DurableClient] IDurableEntityClient entityClient,
            [DurableClient] IDurableOrchestrationClient orchestrationClient,
            string name,
            ILogger log)
        {
            try
            {
                #region blob storage trigger option
                //StreamReader reader = new StreamReader(blob);
                //BatchFile batchFile = JsonConvert.DeserializeObject<BatchFile>(reader.ReadToEnd());

                //// Create entity to batch files
                //string entityName = "BatchEntity";
                //var entityId = new EntityId("FileBatch", entityName);
                //await entityClient.SignalEntityAsync(entityId, "Add", batchFile);

                //// Capture entity state to report current file count
                //var entityState = await entityClient.ReadEntityStateAsync<FileBatch>(entityId);
                //if (entityState.EntityExists)
                //    log.LogInformation($"Processed blob {name}. Current file count: {entityState.EntityState.FileCount}");
                #endregion

                JObject storageEvent = JsonConvert.DeserializeObject<JObject>(queueItem);
                var match = Regex.Match(storageEvent["url"].ToString(), "https://(.*).blob.core.windows.net/(.*)/(.*)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    if (string.Equals(match.Groups[2].Value, _containerName))
                    {
                        log.LogInformation("ProcessQueueMessage function received a new queue message");
                        log.LogDebug($"Message content: {queueItem}");

                        // Create a BlobServiceClient object which will be used to create a container client
                        BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);

                        // Create container client object
                        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

                        // Get blob client object
                        string blobName = match.Groups[3].Value;
                        BlobClient blobClient = containerClient.GetBlobClient(blobName);

                        // Read the blob's contents
                        Stream blobStream = await blobClient.OpenReadAsync();
                        StreamReader reader = new StreamReader(blobStream);
                        BatchFile batchFile = JsonConvert.DeserializeObject<BatchFile>(reader.ReadToEnd());

                        // Create entity to batch files
                        string entityName = "BatchEntity";
                        var entityId = new EntityId("FileBatch", entityName);
                        await entityClient.SignalEntityAsync(entityId, "Add", batchFile);

                        // Capture entity state to report current file count
                        var entityState = await entityClient.ReadEntityStateAsync<FileBatch>(entityId);
                        if (entityState.EntityExists)
                            log.LogInformation($"Processed blob {name}. Current file count: {entityState.EntityState.FileCount}");
                    }
                }
                else
                    log.LogWarning($"Unable to parse blob Url from {storageEvent["url"].ToString()}");
            }
            catch (Exception e)
            {
                log.LogError($"ProcessQueueMessage failed with exception {e}");
            }
        }
    }
}
