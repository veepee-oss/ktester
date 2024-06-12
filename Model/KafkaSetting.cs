using Confluent.Kafka;

namespace KafkaTester.Model
{
    public class KafkaSetting
    {
        public string Name { get; set; }
        public string Brokers { get; set; }
        public string Topic { get; set; }
        public int? NbMaxMessages { get; set; }
        public bool IsSaslActivated { get; set; } = false;
        public SecurityProtocol SecurityProtocol { get; set; }
        public SaslMechanism SaslMechanism { get; set; }
        public string SaslUsername { get; set; }
        public string SaslPassword { get; set; }
        public bool IsGzipActivated { get; set; }
    }
}