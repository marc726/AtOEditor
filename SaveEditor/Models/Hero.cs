using System.Collections.Generic;

namespace AtOSaveEditor.Models
{
    public class Hero
    {
        public string? gameName { get; set; }
        public string? owner { get; set; }
        public string? className { get; set; }
        public int? level { get; set; }
        public int? experience { get; set; }
        public int? hp { get; set; }
        public int? energy { get; set; }
        public int? speed { get; set; }
        public float heroGold { get; set; }
        public float heroDust { get; set; }
        public string? weapon { get; set; }
        public string? armor { get; set; }
        public string? jewelry { get; set; }
        public string? accesory { get; set; }
        public List<string> traits { get; set; } = new List<string>();
        public List<string> cards { get; set; } = new List<string>();

        // Resistance properties
        public int resistSlashing { get; set; }
        public int resistBlunt { get; set; }
        public int resistPiercing { get; set; }
        public int resistFire { get; set; }
        public int resistCold { get; set; }
        public int resistLightning { get; set; }
        public int resistMind { get; set; }
        public int resistHoly { get; set; }
        public int resistShadow { get; set; }
    }
}