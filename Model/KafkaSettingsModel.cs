using System.Collections.Generic;

namespace KafkaTester.Model;

public class KafkaSettingsModel
{
    public KafkaSetting CurrentSetting { get; set; } = new KafkaSetting();
    public Dictionary<string, KafkaSetting> KafkaSettings { get; set; } = new();
}
