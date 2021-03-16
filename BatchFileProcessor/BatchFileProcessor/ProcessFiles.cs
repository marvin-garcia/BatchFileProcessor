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

namespace BatchFileProcessor
{
    public static class ProcessFiles
    {
        [FunctionName("ProcessFiles")]
        public static void Run(
            [EventGridTrigger]EventGridEvent eventGridEvent,
            ILogger log)
        {
            JObject myEvent = JsonConvert.DeserializeObject<JObject>(eventGridEvent.Data.ToString());
            log.LogInformation($"Action: {myEvent["operationName"]}");
            log.LogInformation($"Action: {myEvent["resourceUri"]}");
        }
    }
}
