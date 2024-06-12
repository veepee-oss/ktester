namespace KafkaTester.Model;

public class Options
{
    public FilterSettings Filter { get; set; } = new FilterSettings();
    public KafkaConfig KafkaConfig { get; set; } = new KafkaConfig();
}
