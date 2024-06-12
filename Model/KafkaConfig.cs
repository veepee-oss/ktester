using System.Collections.Generic;

namespace KafkaTester.Model;

public class KafkaConfig
{
    public KafkaSetting CurrentSetting { get; set; } = new KafkaSetting();
    public Dictionary<string, KafkaSetting> ListKafkaSettings { get; set; } = new();
}