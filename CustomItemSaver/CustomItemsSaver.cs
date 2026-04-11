using System.Text.Json;
using MelonLoader;

[assembly: MelonInfo(typeof(SaveItems.CustomItemsSaver), "Custom Item Saver Mod", "1.0.2", "ASavageSwan")]
[assembly: MelonGame("Waseku", "Data Center")]

namespace SaveItems
{
    public class CustomItemsSaver : MelonMod
    {
        public static PresetList SavedPresets = new PresetList();
        private static readonly string SaveFilePath = "UserData/CustomItemPresets.json";

        public override void OnInitializeMelon()
        {
            LoadPresets();
        }

        private static void LoadPresets()
        {
            if (File.Exists(SaveFilePath))
            {
                try
                {
                    string json = File.ReadAllText(SaveFilePath);
                    SavedPresets = JsonSerializer.Deserialize<PresetList>(json);
                    if (SavedPresets == null) SavedPresets = new PresetList();

                    // Remove duplicates left over from the pre-dedup bug
                    var seen = new HashSet<string>();
                    var deduped = new List<PresetData>();
                    foreach (var p in SavedPresets.presets)
                    {
                        string key = $"{p.itemID}|{p.itemType}|{p.colorHex}";
                        if (seen.Add(key)) deduped.Add(p);
                    }
                    if (deduped.Count != SavedPresets.presets.Count)
                    {
                        MelonLogger.Msg($"[SaveItems] Removed {SavedPresets.presets.Count - deduped.Count} duplicate preset(s).");
                        SavedPresets.presets = deduped;
                        SavePresets();
                    }

                    MelonLogger.Msg($"[SaveItems] Loaded {SavedPresets.presets.Count} preset(s).");
                }
                catch (Exception e) { MelonLogger.Error("Load failed: " + e.Message); }
            }
        }

        public static void SavePresets()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(SavedPresets, options);
                File.WriteAllText(SaveFilePath, json);
            }
            catch (Exception e) { MelonLogger.Error("Save failed: " + e.Message); }
        }
    }
}