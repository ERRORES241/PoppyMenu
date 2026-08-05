# Poppy Menu (Enhanced)

An in-game cheat and debug menu for Risk of Rain 2. Press Insert to open it.

A personal project, open source under the MIT license. Pull requests and ideas are welcome.

## Features

Organized into clean top-level tabs and focused sub-pages.

### Aimbot
- Silent aim that hits the target closest to your crosshair on any survivor without moving your camera (works on hitscan and projectiles).
- **Railgunner Weak Point Targeting:** Automatically locks onto enemy red critical weak points (`isSniperTarget`) when actively aiming/scoping with Railgunner skills.
- **No Spread & No Recoil:** Eliminates weapon spread and camera kickback across all survivor skills.
- **Customization:** Target priority (crosshair, distance, low HP, high HP), prioritize bosses, line-of-sight check, FOV/range limits, homing projectiles, magic bullet (penetrate walls), and smooth continuous FOV overlay circle.

### Visuals
- ESP for enemies, interactables, and teleporters through walls.
- **Category Filters:** Toggle ESP rendering independently for Chests/Multishops, Shrines, Drones/Turrets, Printers, Scrappers, Barrels, and Other interactables.
- **Shrine & Pricing Accuracy:** Correct price formatting for shrines (Mountain Shrines, Shrine of Chance, Cleansing Pools) that do not require money, omitting misleading costs.
- **Void Potentials & Cradles:** Distinct purple markers for Void Potentials (OptionPickup) and Void Cradles.
- **Dynamic Localization:** ESP labels use native game language tokens (Russian, English, etc.).
- **Active Effects HUD:** Draggable on-screen HUD displaying currently active toggles (God Mode, Aimbot, Sprint, etc.) with automatic pixel-accurate height scaling.

### Movement
- **Flight & No-Clip:** Free flight movement across the map.
- **Attack-Aware Always Sprint:** Forces sprinting when moving without interrupting attack/skill animations or standing idle.
- **BunnyHop (Auto-Jump) & Jump Pack:** Integrated auto-jumping that preserves Wax Quail momentum and Headstomper mechanics.

### Player
Main controls for the local character across three sub-pages:
- **Survival:** God mode, Semi-Godmode (Buddha), Skills No CD, full heal, hurt, respawn, and instant Kill All Enemies.
- **Economy:** Give Money, XP, and Lunar Coins with configurable numerical stepper inputs.
- **Stats:** Live readout and multipliers for damage, attack speed, movement speed, armor, crit chance, and max health.
- **Items:** Searchable item and equipment giver with tier color coding, Stack Inventory (Shrine of Order), Reroll Items, Clear Inventory, Undo Last Change, and Equipment No Cooldown toggle.

### World
Stage, run, and server management across sub-pages:
- **World:** Match freeze, run timer freeze, time scaling (slow motion/fast forward), No Enemies mode, experience lock, and profile save prevention.
- **Spawn:** Spawn monsters or interactables at crosshairs with team and count parameters. Cleaned catalog names without raw asset tags.
- **Run:** Set stage clear count, run time, team level, toggle active artifacts, and stage skip.
- **Teleporter:** Instant teleporter charge toggle, stage skip, and Mountain Shrine stack adder.
- **Players:** Lobby player controls (heal, revive, hurt, kill, teleport, team change, item grant, kick/ban).

### Fun
Dedicated survivor-specific utilities and mechanics:
- **Instant Ult Charge (Railgunner):** Instant 100% charge completion for Railgunner's ultimate upon activation.
- **No Ult Cooldown (Railgunner):** Resets Special skill cooldown and bypasses the 5-second overheat/backpack lockout without altering primary, secondary, or utility skills.
- **Ult Spam (Railgunner):** Converts primary M1 (LMB) attacks into Supercharge ultimate bullets for continuous un-scoped ultimate firing.

### Settings & Configuration
- **Settings:** Menu keybind, UI scale, host permission toggles, accent color, catalog refresh, and window reset.
- **Keybinds:** Map menu actions, toggles, or macros to keyboard and mouse buttons (including side mouse buttons).
- **Presets & Macros:** Save toggle profiles, export/import preset codes, and build custom multi-action execution chains bound to single hotkeys.

## Console Commands
Compatible with standard in-game console commands used by DebugToolkit: `give_item`, `give_equip`, `give_money`, `give_lunar`, `give_buff`, `remove_item`, `random_items`, `spawn_ai`, `spawn_body`, `spawn_interactable`, `spawn_as`, `no_enemies`, `god`, `buddha`, `noclip`, `kill_all`, `true_kill`, `respawn`, `heal`, `hurt`, `teleport_on_cursor`, `change_team`, `next_stage`, `fixed_time`, `stop_timer`, `charge_zone`, `set_artifact`, `time_scale`, `team_set_level`, and `run_set_stages_cleared`.

## Controls
- Default menu key: `Insert` (rebindable in Settings or Keybinds).
- Click and drag title bars to move windows; drag bottom-right handles to resize.

## Multiplayer
Host permissions apply by default. Server-side changes (items, spawns, match freeze) require host privileges or solo play. Visual and client-side movement features operate locally.

## Installation
Drop `PoppyMenu.dll` into `Risk of Rain 2/BepInEx/plugins` or install via r2modman / Thunderstore Mod Manager.

## Credits
Poppy Menu is maintained by Poppy.
*Note: Recent menu restructuring and enhancement updates were developed with AI assistance.*

Based on code and architecture from Umbra Menu (Aquatic Labs), Spektre Menu (BennettStaley), and Lodington's fork.
Inspired by DebugToolkit (harbingerofme) and Aerolt (Lodington).
Decompiled code analysis made possible with **JetBrains dotPeek**.
