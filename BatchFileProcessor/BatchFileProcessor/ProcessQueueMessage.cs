//using System;
//using System.IO;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Text.RegularExpressions;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using Azure.Storage.Blobs;
//using Microsoft.Extensions.Logging;
//using Microsoft.Azure.WebJobs;
//using Microsoft.Azure.WebJobs.Extensions.DurableTask;

//namespace BatchFileProcessor
//{
//    public static class ProcessQueueMessage
//    {
//        private static string _connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
//        private static string _containerName = Environment.GetEnvironmentVariable("ContainerName");

//        [FunctionName("ProcessQueueMessage")]
//        public static async Task Run(
//            [QueueTrigger("batchfiles")] string queueItem,
//            [DurableClient] IDurableEntityClient entityClient,
//            [DurableClient] IDurableOrchestrationClient orchestrationClient,
//            ILogger log)
//        {
//            try
//            {
//                log.LogInformation("ProcessQueueMessage function received a new queue message");
//                log.LogDebug($"Message content: {queueItem}");

//                JObject storageEvent = JsonConvert.DeserializeObject<JObject>(queueItem);
//                var match = Regex.Match(storageEvent["url"].ToString(), "https://(.*).blob.core.windows.net/(.*)/(.*)", RegexOptions.IgnoreCase);
//                if (match.Success)
//                {
//                    if (string.Equals(match.Groups[1].Value, _containerName))
//                    {
//                        // Create a BlobServiceClient object which will be used to create a container client
//                        BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);

//                        // Create container client object
//                        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

//                        // Get a reference to a blob
//                        string blobName = match.Groups[2].Value;
//                        BlobClient blobClient = containerClient.GetBlobClient(blobName);

//                        // Read the blob's contents
//                        Stream blobStream = await blobClient.OpenReadAsync();
//                        StreamReader reader = new StreamReader(blobStream);
//                        BatchFile batchFile = JsonConvert.DeserializeObject<BatchFile>(reader.ReadToEnd());

//                        // Trigger entity based on current time
//                        string entityName = "BatchEntity";
//                        var entityId = new EntityId("FileBatch", entityName);
//                        var state = await entityClient.ReadEntityStateAsync<FileBatch>(entityId);
//                        await entityClient.SignalEntityAsync(entityId, "Add", batchFile);

//                        string instanceId = await orchestrationClient.StartNewAsync("MonitorBatchStatus", entityName);

//                        log.LogInformation($"Processed blob {blobName}");
//                    }
//                }
//                else
//                    log.LogWarning($"Unable to parse blob Url from {storageEvent["url"].ToString()}");
//            }
//            catch (Exception e)
//            {
//                log.LogError($"ProcessQueueMessage failed with exception {e}");
//            }
//        }

//        [FunctionName("MonitorBatchStatus")]
//        public static async Task MonitorBatchStatus(
//            [OrchestrationTrigger] IDurableOrchestrationContext context,
//            [CosmosDB(
//                databaseName: "%CosmosDatabaseName%",
//                collectionName: "%CosmosCollectionName%",
//                ConnectionStringSetting = "CosmosDBConnection")]IAsyncCollector<FileBatch> cosmosDBOut,
//            ILogger log)
//        {
//            try
//            {
//                var entityId = new EntityId("FileBatch", context.InstanceId);
//                var fileBatch = await context.CallEntityAsync<FileBatch>(entityId, "Get");

//                TimeSpan timeout = TimeSpan.FromSeconds(30);
//                DateTime deadline = fileBatch.LastUpdated.Add(timeout);

//                if (DateTime.UtcNow < fileBatch.LastUpdated.AddSeconds(30))
//                {
//                    await context.CallEntityAsync<FileBatch>(entityId, "Reset");
//                }

//                using (var cts = new CancellationTokenSource())
//                {
//                    Task<FileBatch> entityTask = context.CallEntityAsync<FileBatch>(entityId, "WhenPastDue");
//                    //Task timeoutTask = context.CreateTimer(deadline, cts.Token);

//                    Task<FileBatch> pastDue = await Task.WhenAny(entityTask);
//                    fileBatch = await pastDue;

//                    await context.CallEntityAsync<FileBatch>(entityId, "Reset");
//                    cts.Cancel();

//                    await cosmosDBOut.AddAsync(fileBatch);

//                    log.LogInformation($"File batch with Id {fileBatch.Id} was added to DB");
//                }
//            }
//            catch (Exception e)
//            {
//                log.LogError($"MonitorBatchStatus failed with exception {e}");
//            }
//        }
//    }
//}
