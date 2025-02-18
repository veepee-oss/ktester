using System;
using System.Collections.Generic;

namespace KafkaTester.Model;

public class KafkaMessage
{
    public int Partition { get; set; }

    public long Offset { get; set; }

    public byte[] Key { get; set; }

    public byte[] Message { get; set; }

    public List<KafkaHeader> Headers { get; set; } = new();
    public DateTime MessageDateTime { get; set; }
}

public class KafkaHeader
{
    public string Key { get; set; }
    public string Value { get; set; }
}
