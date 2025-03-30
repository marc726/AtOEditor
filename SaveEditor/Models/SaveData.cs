using System;

namespace AtOSaveEditor.Models
{
    [Serializable]
    public class SaveData
    {
        public string? GameDate { get; set; }
        public string? CurrentMapNode { get; set; }
        // TeamAtO field is stored as a JSON string.
        public string? TeamAtO { get; set; }
        public int GameMode { get; set; }
    }
}