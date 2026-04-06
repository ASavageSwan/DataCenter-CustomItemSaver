using HarmonyLib;
using Il2Cpp;
using Il2CppPolyAndCode.UI;
using UnityEngine;
using UnityEngine.UI;
using Il2CppTMPro;
using Il2CppInterop.Runtime;
using Object = UnityEngine.Object;

namespace SaveItems
{
    [HarmonyPatch(typeof(AssetManagement))] 
    public static class ShopUIHandler
    {
        // 1. Tell the recycler how many extra items we have
        [HarmonyPatch(nameof(AssetManagement.GetItemCount))]
        [HarmonyPostfix]
        public static void Postfix_GetItemCount(ref int __result, AssetManagement __instance)
        {
            if (IsShop(__instance))
                __result += CustomItemsSaver.savedPresets.presets.Count;
        }

        // 2. Inject our data into the recycled cards
        [HarmonyPatch(nameof(AssetManagement.SetCell))]
        [HarmonyPrefix]
        public static bool Prefix_SetCell(ICell cell, int index, AssetManagement __instance)
        {
            if (!IsShop(__instance)) return true;

            int originalCount = __instance.GetItemCount() - CustomItemsSaver.savedPresets.presets.Count;

            if (index >= originalCount)
            {
                int presetIndex = index - originalCount;
                if (presetIndex < CustomItemsSaver.savedPresets.presets.Count)
                {
                    var preset = CustomItemsSaver.savedPresets.presets[presetIndex];
                    GameObject card = cell.Cast<Component>().gameObject;
                    ApplyPresetVisuals(card, preset);
                    return false; // Stop original game logic
                }
            }
            return true;
        }

        private static bool IsShop(AssetManagement instance) => 
            instance.GetComponentInParent<ComputerShop>() != null || instance.name.Contains("Shop");

        private static void ApplyPresetVisuals(GameObject card, PresetData preset)
        {
            // CRITICAL: Destroy the original script so it doesn't overwrite our text
            var si = card.GetComponent<ShopItem>();
            if (si != null) Object.DestroyImmediate(si);

            // Text Setup (Using TMP_Text to be safe across all TMPro types)
            foreach (var txt in card.GetComponentsInChildren<TMP_Text>(true))
            {
                string n = txt.name.ToLower();
                if (n.Contains("price") || txt.text.Contains("$"))
                    txt.text = $"{preset.price} $";
                else
                    txt.text = string.IsNullOrEmpty(preset.displayName) ? "Custom Item" : preset.displayName;
            }

            // Image & Icon Setup
            Sprite icon = FindItemSprite(preset.itemID, preset.itemType);
            foreach (var img in card.GetComponentsInChildren<Image>(true))
            {
                string n = img.name.ToLower();
                if (n.Contains("icon") || n.Contains("item"))
                {
                    if (icon != null) img.sprite = icon;
                    img.color = Color.white;
                }
                else if (n.Contains("bg") || n.Contains("frame") || img.gameObject == card)
                {
                    if (ColorUtility.TryParseHtmlString(preset.colorHex, out Color c)) img.color = c;
                }
            }

            // Button Logic
            var btn = card.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                var shop = Object.FindObjectOfType<ComputerShop>();
                System.Action buy = new System.Action(() => {
                    if (ColorUtility.TryParseHtmlString(preset.colorHex, out Color f)) {
                        shop.SpawnNewCartItem(preset.itemID, preset.price, (PlayerManager.ObjectInHand)preset.itemType, preset.displayName, new Il2CppSystem.Nullable<Color>(f));
                        shop.UpdateCartTotal();
                    }
                });
                btn.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction>(buy));
            }
        }

        private static Sprite FindItemSprite(int id, int type)
        {
            foreach (var so in Resources.FindObjectsOfTypeAll<ShopItemSO>())
                if (so.itemID == id && (int)so.itemType == type) return so.sprite;
            return null;
        }
    }
}