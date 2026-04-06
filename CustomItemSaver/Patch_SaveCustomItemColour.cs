using HarmonyLib;
using Il2Cpp;
using UnityEngine;
using MelonLoader;

namespace SaveItems
{
    [HarmonyPatch(typeof(ComputerShop), nameof(ComputerShop.ButtonChosenColor))]
    public class Patch_SaveCustomItemColour
    {
        public static void Prefix(ComputerShop __instance)
        {
            if (__instance.flexibleColorPicker != null && __instance.isPendingColorPurchase)
            {
                Color chosenColor = __instance.flexibleColorPicker.color;
                string hex = ColorUtility.ToHtmlStringRGB(chosenColor);

                PresetData newPreset = new PresetData
                {
                    colorHex = "#" + hex,
                    itemID = __instance.pendingItemID,
                    itemType = (int)__instance.pendingItemType,
                    price = __instance.pendingPrice,
                    displayName = __instance.pendingDisplayName
                };

                CustomItemsSaver.savedPresets.presets.Add(newPreset);
                CustomItemsSaver.SavePresets();
            }
        }

        public static void Postfix(ComputerShop __instance)
        {
            // Re-inject all preset cards so the newly saved one appears immediately
            Patch_AddColourToShop.InjectPresetCards(__instance);
        }
    }
}
