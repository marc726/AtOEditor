namespace AtOSaveEditor.Models
{
    public class CardInfo
    {
        public string? Name { get; set; }
        public string? Category { get; set; }
        public string? FilePath { get; set; }
        public override string? ToString() => Name;
    }
}