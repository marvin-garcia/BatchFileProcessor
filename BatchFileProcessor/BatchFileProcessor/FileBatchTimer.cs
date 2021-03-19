using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace BatchFileProcessor
{
    public static class FileBatchTimer
    {
        [FunctionName("FileBatchTimer")]
        public static async Task Run(
            [TimerTrigger("*/30 * * * * *")] TimerInfo myTimer,
            [DurableClient] IDurableEntityClient entityClient,
            [DurableClient] IDurableOrchestrationClient orchestrationClient,
            [CosmosDB(
                databaseName: "%CosmosDatabaseName%",
                collectionName: "%CosmosCollectionName%",
                ConnectionStringSetting = "CosmosDBConnection")]IAsyncCollector<FileBatch> cosmosDBOut,
            ILogger log)
        {
            try
            {
                // Get entity state
                //string entityName = "BatchEntity";
                //var entityId = new EntityId("FileBatch", entityName);
                var entityList = await entityClient.ListEntitiesAsync(new EntityQuery(), new CancellationToken());

                foreach (var entity in entityList.Entities)
                {
                    var entityState = await entityClient.ReadEntityStateAsync<FileBatch>(entity.EntityId);

                    // Check if entity exists and is outdated
                    if (entityState.EntityExists)
                    {
                        if (entityState.EntityState.IsActive && entityState.EntityState.IsPastDue)
                        {
                            log.LogInformation($"FileBatchTimer will reset entity because it is outdated");

                            // Write batch to DB
                            await cosmosDBOut.AddAsync(entityState.EntityState);

                            // Signal entity deletion
                            await entityClient.SignalEntityAsync(entity.EntityId, "Delete");
                            await entityClient.CleanEntityStorageAsync(true, true, new CancellationToken());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.LogError($"FileBatchTimer failed with exception {e}");
            }
        }
    }
}
