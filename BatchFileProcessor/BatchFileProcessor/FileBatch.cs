using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BatchFileProcessor
{
    public class FileBatch
    {
        [JsonProperty("startTime")]
        public DateTime StartTime { get; set; }
        [JsonProperty("endTime")]
        public DateTime EndTime { get; set; }
        [JsonProperty("files")]
        public List<BatchFile> Files { get; set; }

        public FileBatch()
        {
            this.Files = new List<BatchFile>() { };
        }

        public void Add(BatchFile file) => this.Files.Add(file);
        public void Reset() => this.Files = new List<BatchFile>() { };
        public List<BatchFile> GetFiles() => this.Files;
    }

    public class BatchFile
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
        [JsonProperty("threshold")]
        public int Threshold { get; set; }
        [JsonProperty("value")]
        public int Value { get; set; }
    }
}
