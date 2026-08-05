# Poppy Menu (Enhanced)

An in-game cheat and debug menu for Risk of Rain 2. Press Insert to open it.

A personal project, open source under the MIT license. Pull requests and ideas are welcome. There's a lot packed in here.

## What it does

Organized into tabs, with sub-pages where it makes sense.

### Aimbot
- Silent aim that hits the target closest to your crosshair on any survivor without your camera moving (works on hitscan, yes including Railgunner, and on projectiles).
  - **Railgunner Weak Point Targeting:** Automatically locks onto enemy red critical weak points (`isSniperTarget`) when playing as Railgunner, delivering 100% critical hits.
  - **No Spread:** Eliminates all bullet and projectile spread across all survivor skills and weapons.
  - **No Recoil:** Suppresses camera recoil and kickback when firing weapons.
  - **Settings:** Target priority (crosshair, distance, low HP, high HP), prioritize bosses, sticky target, line-of-sight check, range and FOV limits, automatic homing projectiles, magic bullet (shots pass through walls), target highlight, smooth continuous FOV circle, and a bindable hold-to-aim key

### Visuals
- ESP for enemies, interactables, and the teleporter through walls.
- Custom colors per category, a max-distance filter, marker size, and toggles for names, distance, and enemy health.
- **Dynamic Localization:** ESP labels pull directly from your game's language settings (Russian, English, etc.).
- **Extensive Tracking:** Supports tracking Void Cradles, Void Potentials, Command Artifact Cubes (with rarity-based coloring), Scrappers, Cleansing Pools, Bazaar Seers/Cauldrons, and more.
- **Camera & FOV:** Built-in FOV override slider (60 - 140).
- **Active Effects HUD:** A perfectly aligned, draggable on-screen HUD that shows your currently active cheats (God Mode, Aimbot, etc.). Can be toggled on/off.

### Movement
- Flight, no-clip, always sprint, jump pack. You can still shoot and use skills while flying.
- **Native BunnyHop (Auto-Jump):** Flawless integration into the game's native input pipeline. Holding jump perfectly triggers the game's built-in mechanics (including Wax Quail momentum and Headstomper buffs) without desyncs.

### Player
Sub-pages for everything related to your local character:
- General: god mode, buddha (survive lethal hits), infinite skills, heal to full or by an amount, hurt yourself, respawn, give money/XP/lunar coins, give or remove any buff, and perform quick actions like giving all items or killing all enemies.
- Stats: multipliers for damage, attack speed, move speed, armor, crit, and max health, with a live readout.
- Items: Give any item or equipment from a searchable, tier-colored list. Give all items (the best quality of each), stack inventory (like Shrine of Order), reroll items, clear inventory, undo the last inventory change, and toggle equipment cooldown.
- Character: Turn into any survivor or monster. Pick something that can't actually spawn and it just puts you back instead of ending your run.

### World
Sub-pages for everything stage, difficulty, and lobby related:
- Time: freeze the whole match, freeze only the run timer so difficulty stops climbing, or scale time for slow motion and fast forward. Plus safety toggles: no enemies (killed on spawn), lock experience, and prevent profile saving so test runs stay off your save file.
- Spawn: spawn any monster or interactable where you aim, choose how many and whether they're your ally or an enemy.
- Run: set stages cleared, run time, and team level, toggle every artifact on or off, jump to any stage, with a live readout of stage, clock, and difficulty.
- Teleporter: instant charge, skip stage, add a mountain shrine stack, spawn the shop, gold, or celestial portals.
- Players: Everyone in the lobby with per-player controls (heal, revive, hurt, kill, teleport, change team, give items, change body, kick/ban).

### Settings
Four sub-pages:
- Settings: the Open Menu key, UI scale, the host-permission toggles, the accent color, plus catalog refresh and window reset.
- Keybinds: bind any feature (or a macro) to any key or mouse button (side buttons included). Bindings save to disk and fire while the menu is closed.
- Presets: save your toggles and an item list as a named profile. Auto-grant it on every spawn, or set one as your default to load on startup so reopening the game restores your setup. Export a preset to a code to share it, or import one from a pasted code.
- Macros: build a named sequence of any custom actions (give specific items or buffs, currency, spawn, become a body, run features) and bind it to a single key, so one press fires the whole chain.

### Look and feel
- Draggable, resizable windows with a smooth corner grab, UI scaling, a recolorable accent, on-screen popups when you do something, and it remembers where you left the window.

## Console commands
Everything is also driveable from the normal in-game console using the same command names as DebugToolkit, so you can type what you already know: give_item, give_equip, give_money, give_lunar, give_buff, remove_item, random_items, spawn_ai, spawn_body, spawn_interactable, spawn_as, spawn_portal, add_portal, no_enemies, god, buddha, noclip, kill_all, true_kill, respawn, heal, hurt, teleport_on_cursor, change_team, next_stage, fixed_time, stop_timer, charge_zone, set_artifact, time_scale, team_set_level, run_set_stages_cleared, the list_ commands, and the midgame/lategame/dtzoom macros. The in-menu Console tab has a searchable reference. If you also have DebugToolkit installed, its commands take priority and nothing clashes.

## Controls
- Insert opens and closes the menu (rebindable).
- Click with your mouse, drag the title bar to move a window, drag the bottom-right corner to resize.
- Bind any feature to any key in the Keybinds tab.

## Multiplayer
By default nobody else can use this in your game. If you're the host, requests from other clients are ignored unless you turn on "Allow Others To Use This" in Settings. Anything that changes the actual run, like items, spawns, or freezing the match, only works for the host or in solo. Visual and movement stuff runs for whoever is using it. Keep it to solo, or get the host's okay first.

## Install
The easy way is a mod manager like r2modman or Thunderstore Mod Manager. Install Poppy Menu and it grabs BepInEx and R2API for you. To do it by hand, drop PoppyMenu.dll into Risk of Rain 2/BepInEx/plugins.

## Fair warning
This covers a ton of ground and a lot of it was built fast, so expect rough edges and bugs. Some projectiles and bodies behave differently than others, and a few things are best effort. It's a powerful debug tool, so keep it to single-player or private lobbies and don't ruin anyone's run. No warranty, use it at your own risk.

## Feedback
I'd genuinely love to hear suggestions, feature ideas, and bug reports. If there's something you want added, or something's acting up, drop a comment and let me know. I'm always happy to keep improving it.

## Credits
Poppy Menu is made by Poppy. 
**Note: Recent enhancements to this project were developed with the assistance of AI.**

It started as a rebrand and rewrite of Umbra Menu by Aquatic Labs, which was itself a fork of Spektre Menu by BennettStaley, merged with Lodington's fork. Big thanks to all of them for the original work this is built on.

A lot of the debug and lobby features here are reimplementations inspired by two excellent open mods, go support them:
- DebugToolkit by harbingerofme (BSD-3-Clause)
- Aerolt by Lodington (MIT)

Reverse-engineering and code inspection were made possible with **JetBrains dotPeek**.

Built with BepInEx and R2API, plus the wider Risk of Rain 2 modding community's tools.
