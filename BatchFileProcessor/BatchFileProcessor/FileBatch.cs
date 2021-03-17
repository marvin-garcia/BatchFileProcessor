using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Threading;

namespace BatchFileProcessor
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class FileBatch
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("isActive")]
        public bool IsActive { get; set; }
        [JsonProperty("startTime")]
        public DateTime StartTime { get; set; }
        [JsonProperty("lastUpdated")]
        public DateTime LastUpdated { get; set; }
        [JsonProperty("files")]
        public List<BatchFile> Files { get; set; }

        public void Add(BatchFile file)
        {
            // Initialize time
            if (this.StartTime == null)
            {
                this.IsActive = true;
                this.StartTime = DateTime.UtcNow;
                this.Id = Guid.NewGuid().ToString();
            }

            // Add file to list
            if (this.Files == null)
                this.Files = new List<BatchFile>() { file };
            else
                this.Files.Add(file);

            this.LastUpdated = DateTime.UtcNow;
        }

        public FileBatch Get()
        {
            return this;
        }

        public List<BatchFile> GetFiles()
        {
            return this.Files;
        }

        public int FileCount()
        {
            return this.Files.Count;
        }

        public FileBatch WhenPastDue()
        {
            do
            {
                Thread.Sleep(new TimeSpan(0, 0, 30));
            }
            while (DateTime.UtcNow < this.LastUpdated.AddSeconds(30));
            
            return this;
        }

        public Task Reset()
        {
            this.IsActive = false;
            this.Files = new List<BatchFile>() { };
            Entity.Current.DeleteState();
            return Task.CompletedTask;
        }

        [FunctionName(nameof(FileBatch))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx) => ctx.DispatchAsync<FileBatch>();
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class BatchFile
    {
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
        [JsonProperty("threshold")]
        public int Threshold { get; set; }
        [JsonProperty("value")]
        public int Value { get; set; }
    }
}
