// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName=ProcessFiles
// Use command below for local debugging (https://docs.microsoft.com/en-us/azure/azure-functions/functions-debug-event-grid-trigger-local)
// ngrok http -host-header=localhost 7071

using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;

namespace BatchFileProcessor
{
    [StorageAccount("AzureWebJobsStorage")]
    public static class ProcessEvents
    {
        [FunctionName("ProcessEvents")]
        [return: Queue("batchfiles")]
        public static string Run(
            [EventGridTrigger]EventGridEvent eventGridEvent,            
            ILogger log)
        {
            return eventGridEvent.Data.ToString();
        }
    }
}
