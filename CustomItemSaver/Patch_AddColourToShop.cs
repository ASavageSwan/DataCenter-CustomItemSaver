using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace SaveItems
{
    // UpdateVisualState fires after isUnlocked is definitively set on the ShopItem.
    // We use this as the hook to refresh preset cards rather than UnlockButton,
    // which fires before the async unlock flow has finished updating the field.
    [HarmonyPatch(typeof(ShopItem), nameof(ShopItem.UpdateVisualState))]
    public class Patch_RefreshPresetsOnUnlock
    {
        public static void Postfix(ShopItem __instance)
        {
            // Only act when an item is (or just became) unlocked
            if (!__instance.isUnlocked) return;

            // Only act while the shop UI is actually open
            ComputerShop shop = UnityEngine.Object.FindObjectOfType<ComputerShop>();
            if (shop == null || shop.shopItemParent == null || !shop.shopItemParent.activeInHierarchy)
                return;

            Patch_AddColourToShop.InjectPresetCards(shop);
        }
    }

    [HarmonyPatch(typeof(ComputerShop), nameof(ComputerShop.ButtonShopScreen))]
    public class Patch_AddColourToShop
    {
        // Stored once so re-opening the shop doesn't compound the height each time
        private static float _originalContentHeight = -1f;

        public static void Postfix(ComputerShop __instance)
        {
            InjectPresetCards(__instance);
        }

        public static void InjectPresetCards(ComputerShop shop)
        {
            if (CustomItemsSaver.savedPresets.presets.Count == 0) return;

            GameObject parent = shop.shopItemParent;
            if (parent == null)
            {
                MelonLogger.Warning("[SaveItems] shopItemParent is null");
                return;
            }

            // Find "HL Mods" — the empty horizontal row at the bottom of VL-ShopItems
            // This is the dedicated section for our preset cards
            Transform hlMods = FindChildByName(parent.transform, "HL Mods");
            if (hlMods == null)
            {
                MelonLogger.Warning("[SaveItems] Could not find 'HL Mods' section");
                return;
            }

            // Remove any previously injected cards to avoid duplicates on re-open
            for (int i = hlMods.childCount - 1; i >= 0; i--)
            {
                Transform child = hlMods.GetChild(i);
                if (child.name.StartsWith("PresetCard_"))
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }

            // Clone template from first real ShopItem
            var shopItemsArray = shop.shopItems;
            if (shopItemsArray == null || shopItemsArray.Length == 0)
            {
                MelonLogger.Warning("[SaveItems] shop.shopItems is empty — no template");
                return;
            }
            GameObject templateGO = shopItemsArray[0].gameObject;

            var allSOs = Resources.FindObjectsOfTypeAll<ShopItemSO>();
            var presets = CustomItemsSaver.savedPresets.presets;

            for (int i = 0; i < presets.Count; i++)
            {
                PresetData preset = presets[i];
                GameObject card = UnityEngine.Object.Instantiate(templateGO, hlMods);
                card.name = $"PresetCard_{i}";

                // DestroyImmediate so ShopItem.Start() never runs and resets our values
                var si = card.GetComponent<ShopItem>();
                if (si != null) UnityEngine.Object.DestroyImmediate(si);

                // Text — hierarchy shows children named "Text" and "TextPrice"
                foreach (var txt in card.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    string n = txt.name.ToLower();
                    if (n == "textprice")
                        txt.text = $"{preset.price} $";
                    else if (n == "text")
                        txt.text = preset.displayName;
                }

                // Find matching icon sprite
                Sprite icon = null;
                foreach (var so in allSOs)
                {
                    if (so != null && so.itemID == preset.itemID && (int)so.itemType == preset.itemType)
                    {
                        icon = so.sprite;
                        break;
                    }
                }

                // Images — "bcg" gets the preset colour, "Image" gets the sprite
                foreach (var img in card.GetComponentsInChildren<Image>(true))
                {
                    string n = img.name.ToLower();
                    if (n == "bcg")
                    {
                        if (ColorUtility.TryParseHtmlString(preset.colorHex, out Color c))
                            img.color = c;
                    }
                    else if (n == "image")
                    {
                        if (icon != null) img.sprite = icon;
                        img.color = Color.white;
                    }
                }

                // Check unlock state — the preset should only be buyable if the base item
                // has already been unlocked by the player (XP requirement met).
                ShopItem baseShopItem = FindMatchingShopItem(shop, preset.itemID, preset.itemType);
                bool unlocked = baseShopItem != null && baseShopItem.isUnlocked;

                // Button — shop cards use ButtonExtended, not Button
                var btnExt = card.GetComponentInChildren<ButtonExtended>(true);
                if (btnExt != null)
                {
                    btnExt.onClick.RemoveAllListeners();
                    btnExt.interactable = unlocked;

                    if (unlocked)
                    {
                        PresetData cap = preset;
                        System.Action buyAction = () =>
                        {
                            if (ColorUtility.TryParseHtmlString(cap.colorHex, out Color f))
                            {
                                shop.SpawnNewCartItem(
                                    cap.itemID,
                                    cap.price,
                                    (PlayerManager.ObjectInHand)cap.itemType,
                                    cap.displayName,
                                    new Il2CppSystem.Nullable<Color>(f));
                                shop.UpdateCartTotal();
                            }
                        };
                        btnExt.onClick.AddListener(
                            DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction>(buyAction));
                    }
                }

                // Grey out the whole card when locked so it's visually distinct
                if (!unlocked)
                {
                    foreach (var img in card.GetComponentsInChildren<Image>(true))
                        img.color = new Color(img.color.r * 0.4f, img.color.g * 0.4f, img.color.b * 0.4f, img.color.a);
                    foreach (var txt in card.GetComponentsInChildren<TextMeshProUGUI>(true))
                        txt.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                }

                card.SetActive(true);
            }

            // --- Expand Content to show all injected cards ---
            ScrollRect sr = parent.GetComponentInParent<ScrollRect>();
            if (sr?.content != null)
            {
                RectTransform contentRT = sr.content;

                // Store the game's own height once — prevents compounding on re-open
                if (_originalContentHeight < 0f)
                    _originalContentHeight = contentRT.sizeDelta.y;

                // Disable ContentSizeFitter on Content and VL-ShopItems so neither
                // can overwrite our explicit sizeDelta after we set it.
                var csf = contentRT.GetComponent<ContentSizeFitter>();
                if (csf != null) csf.enabled = false;
                var parentCsf = parent.GetComponent<ContentSizeFitter>();
                if (parentCsf != null) parentCsf.enabled = false;

                float cardHeight = templateGO.GetComponent<RectTransform>()?.rect.height ?? 150f;
                if (cardHeight <= 0f) cardHeight = 150f;

                // Calculate how much vertical space HL Mods needs.
                // If it has a GridLayoutGroup we can compute exact row count.
                // Otherwise fall back to worst-case (every card in its own row).
                float hlNeededHeight;
                var hlGrid = hlMods.GetComponent<GridLayoutGroup>();
                if (hlGrid != null)
                {
                    int cols = (hlGrid.constraint == GridLayoutGroup.Constraint.FixedColumnCount && hlGrid.constraintCount > 0)
                               ? hlGrid.constraintCount : 4;
                    int rows = Mathf.CeilToInt((float)presets.Count / cols);
                    hlNeededHeight = rows * hlGrid.cellSize.y
                                   + Mathf.Max(0, rows - 1) * hlGrid.spacing.y
                                   + hlGrid.padding.top + hlGrid.padding.bottom;
                }
                else
                {
                    // No grid — use card height × count as a safe upper bound
                    hlNeededHeight = presets.Count * (cardHeight + 5f);
                }

                Vector2 sd = contentRT.sizeDelta;
                sd.y = _originalContentHeight + hlNeededHeight + 40f;
                contentRT.sizeDelta = sd;
            }
        }

        // Returns the first ShopItem in the scene whose SO matches itemID + itemType,
        // or null if not found. isUnlocked on the returned instance reflects whether
        // the player has met the XP requirement for that item.
        private static ShopItem FindMatchingShopItem(ComputerShop shop, int itemID, int itemType)
        {
            if (shop.shopItems == null) return null;
            foreach (var si in shop.shopItems)
            {
                if (si?.shopItemSO != null &&
                    si.shopItemSO.itemID == itemID &&
                    (int)si.shopItemSO.itemType == itemType)
                    return si;
            }
            return null;
        }

        // Breadth-first search for a child by exact name anywhere in the hierarchy
        private static Transform FindChildByName(Transform root, string name)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == name) return child;
                Transform found = FindChildByName(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
