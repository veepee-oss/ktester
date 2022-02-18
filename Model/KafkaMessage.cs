
using System.Collections.Generic;

namespace KafkaTester.Model
{
    public class KafkaMessage
    {
        public int Partition { get; set; }

        public long Offset { get; set; }

        public string Message { get; set; }

        public Dictionary<string, string> Headers { get; set; }
    }
}
