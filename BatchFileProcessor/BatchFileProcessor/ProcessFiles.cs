// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Azure.Storage.Blobs;
using System.IO;
using Azure.Storage.Blobs.Models;
using System.Threading.Tasks;

namespace BatchFileProcessor
{
    public static class ProcessFiles
    {
        private static string _connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        private static string _containerName = Environment.GetEnvironmentVariable("ContainerName");

        [FunctionName("ProcessFiles")]
        public static async Task Run(
            [EventGridTrigger]EventGridEvent eventGridEvent,
            ILogger log)
        {
            try
            {
                JObject myEvent = JsonConvert.DeserializeObject<JObject>(eventGridEvent.Data.ToString());
                var match = Regex.Match(myEvent["url"].ToString(), "https://(.*).blob.core.windows.net/(.*)/(.*)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    if (string.Equals(match.Groups[1].Value, _containerName))
                    {
                        // Create a BlobServiceClient object which will be used to create a container client
                        BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);

                        // Create container client object
                        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

                        // Get a reference to a blob
                        string blobName = match.Groups[2].Value;
                        BlobClient blobClient = containerClient.GetBlobClient(blobName);

                        // Read the blob's contents
                        Stream blobStream = await blobClient.OpenReadAsync();
                        StreamReader reader = new StreamReader(blobStream);
                        JObject blob = JsonConvert.DeserializeObject<JObject>(reader.ReadToEnd());

                        log.LogInformation($"Processed blob {blobName}");
                    }
                }
                else
                    log.LogWarning($"Unable to parse blob Url from {myEvent["url"].ToString()}");
            }
            catch (Exception e)
            {
                log.LogError($"ProcessFile failed with exception {e}");
            }
        }
    }
}
