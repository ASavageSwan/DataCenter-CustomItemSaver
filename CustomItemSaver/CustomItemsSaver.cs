using System.Text.Json;
using MelonLoader;

[assembly: MelonInfo(typeof(SaveItems.CustomItemsSaver), "Custom Item Saver Mod", "1.0.0", "ASavageSwan")]
[assembly: MelonGame("Waseku", "Data Center")]

namespace SaveItems
{
    public class CustomItemsSaver : MelonMod
    {
        public static PresetList savedPresets = new PresetList();
        public static string saveFilePath = "UserData/CustomItemPresets.json";

        public override void OnInitializeMelon()
        {
            LoadPresets();
        }

        private static void LoadPresets()
        {
            if (File.Exists(saveFilePath))
            {
                try
                {
                    string json = File.ReadAllText(saveFilePath);
                    savedPresets = JsonSerializer.Deserialize<PresetList>(json);
                    if (savedPresets == null) savedPresets = new PresetList();

                    // Remove duplicates left over from the pre-dedup bug
                    var seen = new HashSet<string>();
                    var deduped = new List<PresetData>();
                    foreach (var p in savedPresets.presets)
                    {
                        string key = $"{p.itemID}|{p.itemType}|{p.colorHex}";
                        if (seen.Add(key)) deduped.Add(p);
                    }
                    if (deduped.Count != savedPresets.presets.Count)
                    {
                        MelonLogger.Msg($"[SaveItems] Removed {savedPresets.presets.Count - deduped.Count} duplicate preset(s).");
                        savedPresets.presets = deduped;
                        SavePresets();
                    }

                    MelonLogger.Msg($"[SaveItems] Loaded {savedPresets.presets.Count} preset(s).");
                }
                catch (Exception e) { MelonLogger.Error("Load failed: " + e.Message); }
            }
        }

        public static void SavePresets()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(savedPresets, options);
                File.WriteAllText(saveFilePath, json);
            }
            catch (Exception e) { MelonLogger.Error("Save failed: " + e.Message); }
        }
    }
}