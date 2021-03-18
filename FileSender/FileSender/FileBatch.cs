using System;
using Newtonsoft.Json;

namespace FileSender
{
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
