namespace KafkaTester.Model
{
    public class SearchSetting
    {
        public bool IsInvariantCase { get; set; } = false;
        public bool IsCheckKey { get; set; } = true;
        public bool IsCheckMessage { get; set; } = true;
    }
}
