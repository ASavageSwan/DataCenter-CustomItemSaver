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