using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace SaveItems
{
    // Hook into the game's native method for closing the shop
    [HarmonyPatch(typeof(ComputerShop), nameof(ComputerShop.CloseShop))]
    public class Patch_FixColorPickerBug
    {
        public static void Postfix(ComputerShop __instance)
        {
            // Check if the color picker exists and is currently turned on
            if (__instance.flexibleColorPicker != null && __instance.flexibleColorPicker.gameObject.activeSelf)
            {
                // Force the game to run its native cancel logic
                __instance.ButtonCancelColorPicker();
            }
        }
    }
}