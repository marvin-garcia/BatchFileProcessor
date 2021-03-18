using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

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
        [JsonIgnore()]
        private int TimeWindow = Convert.ToInt32(Environment.GetEnvironmentVariable("BatchTimeWindow"));
        [JsonIgnore()]
        public bool IsPastDue
        {
            get
            {
                return this.LastUpdated.AddSeconds(this.TimeWindow) < DateTime.UtcNow;
            }
        }
        [JsonIgnore()]
        public int FileCount
        {
            get
            {
                return this.Files.Count;
            }
        }

        public void Add(BatchFile file)
        {
            if (!this.IsActive)
            {
                this.IsActive = true;
                this.StartTime = DateTime.UtcNow;
                this.Id = Guid.NewGuid().ToString();
                this.Files = new List<BatchFile>() { file };
            }
            else
            {
                this.Files.Add(file);
            }

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

        public FileBatch WhenPastDue()
        {
            do
            {
                Thread.Sleep(new TimeSpan(0, 0, this.TimeWindow));
            }
            while (!this.IsPastDue);

            return this;
        }

        public Task Reset()
        {
            this.IsActive = false;
            this.Files = new List<BatchFile>() { };
            return Task.CompletedTask;
        }

        public void Delete()
        {
            Entity.Current.DeleteState();
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
