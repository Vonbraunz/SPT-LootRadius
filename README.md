# SPT-LootRadius

## Disclaimer - This Version has an item ghosting bug when moving items from the nearby grid to inventory - ive traced it all the way to the root cause of a structural issue in how the panel lifecycle interacts with the game's two-phase event system this is fundamentally unfixable through event plumbing, container ownership, or registration. The listener is destroyed before the completion event arrives. The only real fix would be patching ItemsPanel itself to not close during a move, which is a much bigger undertaking and out of my expertise, The ghost is cosmetic — items move correctly, the visual just lingers until reopen

A port of [DrakiaXYZ's SPT-LootRadius](https://github.com/DrakiaXYZ/SPT-LootRadius) updated for **SPT 4.0.13**.

When you open your inventory in-raid, a **"Nearby Items"** panel automatically appears on the right side showing all lootable items within a configurable radius around your player. Drag items directly from the panel into your inventory without having to walk up and individually interact with each one.

---

## Features

- Shows nearby world loot in the inventory panel automatically when opened in-raid
- Configurable scan radius (default 2m, max 10m)
- Floor detection — also catches items slightly below the floor you're standing on
- Line-of-sight check — only shows items your character can actually see
- Quest items visible in the panel but cannot be dragged (prevents quest tracking issues)
- Live updates — items picked up by other means disappear from the panel in real time

---

## Requirements

- [SPT 4.0.13](https://www.sp-tarkov.com/)
- [BepInEx](https://github.com/BepInEx/BepInEx) (included with SPT)
- Optional: [BepInEx ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) for in-game settings

---

## Installation

1. Download the latest release
2. Drop `DrakiaXYZ-LootRadius.dll` into your `BepInEx/plugins/` folder
3. Launch SPT

---

## Configuration

The loot scan radius can be adjusted two ways:

**In-game** (requires ConfigurationManager):
- Press `F12` to open the settings menu
- Find `DrakiaXYZ-LootRadius` → adjust `Loot Radius`

**Config file:**
- Located at `BepInEx/config/xyz.drakia.lootradius.cfg` (auto-generated on first launch)
```ini
[1. General]
Loot Radius = 2
```
Valid range: `0` – `10` metres

---

## Building from Source

**Requirements:**
- Visual Studio 2022
- SPT 4.0.13 installed

**Steps:**
1. Clone the repo
2. Open `DrakiaXYZ-LootRadius.csproj`
3. Update the `HintPath` entries in the `.csproj` to point to your SPT install directory
4. Build — the post-build event copies the DLL to `BepInEx/plugins/` automatically

---

## Changes from Original (3.10 → 4.0.13)

| File | Change |
|---|---|
| `LootRadiusPlugin.cs` | Bumped version to 1.5.0, updated SPT dependency to 4.0.0 |
| `LootRadiusStashGrid.cs` | Updated all GClass/GStruct numbers for 4.0.13, fixed capitalised field names in GClass3120 |
| `GameStartedPatch.cs` | GStruct126 → GStruct162, fixed C# 7.3 compatibility |
| `LootPanelOpenPatch.cs` | Updated SimpleStashPanel.Show signature, fixed C# 7.3 compatibility |
| `DrakiaXYZ-LootRadius.csproj` | Added Sirenix.Serialization reference, locked LangVersion to 7.3 |

---

## Credits

- **[DrakiaXYZ](https://github.com/DrakiaXYZ)** — original mod author
- **Vonbraunz** — SPT 4.0.13 port

---

## License

MIT — see [LICENSE.txt](LICENSE.txt)
