# DataCenter-CustomItemSaver
A mod for the game Data Center that allows players to save custom colours for items and itens are available in the shop 

---
## ✨ Features
- **Custom Presets:** Automatically saves any item purchased with a custom color.
- **Native UI Integration:** Access your saved items via the **Mods** category in the Computer Shop.
- **Persistent Storage:** Presets are saved to `UserData/CustomItemPresets.json` and persist between game sessions.
---
## 🛠 Requirements
* **Data Center** (Steam version)
* **MelonLoader (Il2Cpp)** - Version 0.6.0 or higher.
---

## 🚀 Installation
1. **Install MelonLoader:**
   - Download the [MelonLoader Installer](https://github.com/LavaGang/MelonLoader/releases).
   - Run the installer and select your `Data Center.exe`.
   - Ensure the version is set to **latest** and the game type is **Il2Cpp**.
   - Click **Install**.

2. **Run the Game Once:**
   - Start the game once to allow MelonLoader to initialize the Il2Cpp assemblies. 
   - Close the game once you reach the main menu.

3. **Install the Mod:**
   - Download the `SaveItems.dll`.
   - Place `SaveItems.dll` into the `Mods` folder in your game directory.

4. **Verify Installation:**
   - Launch the game. The MelonLoader console should show `Saved Items Mod` has loaded successfully.
---

## 🎮 How to Use
### Saving a Preset
1. Open the **Computer Shop**.
2. Select an item and click the **Color Palette** icon to choose a custom color.
3. Purchase the item. The mod will automatically capture the item type, name, price, and color.

### Buying Saved Presets
1. Open the **Computer Shop**.
2. Click the **Mods** button (added to the left-hand category list).
3. Browse your custom items. The cards will reflect the saved colors and prices.
4. Click any item to add it to your cart.
---

## 📂 Technical Details
- **Data File:** `YourGameFolder/UserData/CustomItemPresets.json`. You can delete entries here if you want to remove a preset - requires a restart of game to take effect.
---

## 📜 Credits
- Powered by **MelonLoader** and **HarmonyLib**.
