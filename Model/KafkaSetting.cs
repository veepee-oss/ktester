namespace KafkaTester.Model
{
    public class KafkaSetting
    {
        public string Brokers { get; set; }
        public string Topic { get; set; }
        public string Filter { get; set; }
        public int? NbMaxMessages { get; set; }
    }
}