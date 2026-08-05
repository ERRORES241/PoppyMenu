using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace PoppyMenu
{
    internal static class ActionRegistry
    {
        private static readonly List<PoppyAction> _all = new List<PoppyAction>();
        private static readonly Dictionary<string, PoppyAction> _byId = new Dictionary<string, PoppyAction>();

        internal static IReadOnlyList<PoppyAction> All => _all;

        static ActionRegistry() => Build();

        internal static PoppyAction Get(string id) => id != null && _byId.TryGetValue(id, out var a) ? a : null;

        internal static void Run(string id)
        {
            PoppyAction a = Get(id);
            if (a == null) return;
            try { a.Invoke(); }
            catch (Exception e) { Log.Error($"action {id} failed: {e}"); }
        }

        internal static void RunId(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (id.StartsWith("macro:")) { MacroStore.Run(id.Substring("macro:".Length)); return; }
            Run(id);
        }

        private static void Add(string id, string category, string name, Action fn)
        {
            var a = new PoppyAction(id, category, name, fn);
            _all.Add(a);
            _byId[id] = a;
        }

        private static void Build()
        {
            Add("menu.disableAll", "Menu", "Disable everything", Cheats.DisableAll);

            Add("player.god", "Player", "God mode", () => PlayerModule.GodMode = !PlayerModule.GodMode);
            Add("player.buddha", "Player", "Semi-Godmode", () => Safety.Buddha = !Safety.Buddha);
            Add("player.skills", "Player", "Skills No CD", () => PlayerModule.InfiniteSkills = !PlayerModule.InfiniteSkills);
            Add("player.healFull", "Player", "Heal to full", () => NetUtil.Do(PoppyOp.HealFull));
            Add("player.respawn", "Player", "Respawn", () => NetUtil.Do(PoppyOp.Respawn));

            Add("move.flight", "Movement", "Flight", MovementModule.ToggleFlight);
            Add("move.noclip", "Movement", "No-clip", MovementModule.ToggleNoClip);
            Add("move.sprint", "Movement", "Always sprint", MovementModule.ToggleSprint);
            Add("move.jump", "Movement", "Jump pack", () => MovementModule.JumpPack = !MovementModule.JumpPack);

            Add("aim.toggle", "Aimbot", "Aimbot", () => Aim.Enabled = !Aim.Enabled);
            Add("aim.magic", "Aimbot", "Magic bullet", () => Aim.MagicBullet = !Aim.MagicBullet);

            Add("items.giveAll", "Items", "Give all items", () => NetUtil.Do(PoppyOp.GiveAllItems, i2: 1));
            Add("items.clear", "Items", "Clear inventory", () => NetUtil.Do(PoppyOp.ClearInventory));
            Add("items.stack", "Items", "Stack inventory", () => NetUtil.Do(PoppyOp.StackInventory));
            Add("items.reroll", "Items", "Reroll items", () => NetUtil.Do(PoppyOp.RollItems));
            Add("items.noEquipCd", "Items", "No equipment cooldown", () => ItemsModule.NoEquipmentCooldown = !ItemsModule.NoEquipmentCooldown);

            Add("combat.killAll", "World", "Kill all enemies", () => NetUtil.Do(PoppyOp.KillAllEnemies));
            Add("combat.noEnemies", "Safety", "No enemies", () => Safety.NoEnemies = !Safety.NoEnemies);

            Add("tp.charge", "Teleporter", "Instant charge (toggle)", () => TeleporterModule.InstaCharge = !TeleporterModule.InstaCharge);
            Add("tp.skip", "Teleporter", "Skip stage", () => NetUtil.Do(PoppyOp.SkipStage));
            Add("tp.portals", "Teleporter", "Spawn all portals", () =>
            {
                NetUtil.Do(PoppyOp.SpawnShopPortal);
                NetUtil.Do(PoppyOp.SpawnGoldshoresPortal);
                NetUtil.Do(PoppyOp.SpawnMSPortal);
                TeleporterInteraction tp = TeleporterInteraction.instance;
                Vector3 pos = tp != null ? tp.transform.position + UnityEngine.Random.insideUnitSphere * 5f : (PlayerContext.HasBody ? PlayerContext.Body.footPosition + UnityEngine.Random.insideUnitSphere * 5f : Vector3.zero);
                NetUtil.Do(PoppyOp.Spawn, i1: 1, i2: (int)TeamIndex.Neutral, s1: "iscVoidPortal", f1: pos.x, f2: pos.y, f3: pos.z);
                NetUtil.Do(PoppyOp.Spawn, i1: 1, i2: (int)TeamIndex.Neutral, s1: "iscDeepVoidPortal", f1: pos.x, f2: pos.y, f3: pos.z);
                NetUtil.Do(PoppyOp.Spawn, i1: 1, i2: (int)TeamIndex.Neutral, s1: "iscColossusPortal", f1: pos.x, f2: pos.y, f3: pos.z);
            });

            Add("world.freeze", "World", "Freeze match", () => WorldModule.FreezeMatch = !WorldModule.FreezeMatch);
            Add("world.freezeTimer", "World", "Freeze run timer", () => WorldModule.FreezeTimer = !WorldModule.FreezeTimer);

            Add("esp.enemies", "ESP", "Enemy ESP", () => RenderModule.EspMobs = !RenderModule.EspMobs);
            Add("esp.interactables", "ESP", "Interactable ESP", () => RenderModule.EspInteractables = !RenderModule.EspInteractables);
            Add("esp.teleporter", "ESP", "Teleporter ESP", () => RenderModule.EspTeleporter = !RenderModule.EspTeleporter);

            Add("fun.instaCharge", "Fun", "Instant Ult Charge", () => FunModule.RailgunnerInstaCharge = !FunModule.RailgunnerInstaCharge);
            Add("fun.noUltCooldown", "Fun", "No Ult Cooldown", () => FunModule.RailgunnerNoUltCooldown = !FunModule.RailgunnerNoUltCooldown);
            Add("fun.ultSpam", "Fun", "Ult Spam (Every Shot = Ult)", () => FunModule.RailgunnerUltSpam = !FunModule.RailgunnerUltSpam);

            Add("macro.midgame", "Macros", "Mid-game loadout", () => PoppyConsole.Submit("midgame"));
            Add("macro.lategame", "Macros", "End-game loadout", () => PoppyConsole.Submit("lategame"));
            Add("macro.dtzoom", "Macros", "Zoom items", () => PoppyConsole.Submit("dtzoom"));

            Add("tab.player", "Open tab", "Player tab", () => MenuRoot.SelectTabByName("Player"));
            Add("tab.items", "Open tab", "Items tab", () => MenuRoot.SelectTabByName("Items"));
            Add("tab.players", "Open tab", "Players tab", () => MenuRoot.SelectTabByName("Players"));
            Add("tab.fun", "Open tab", "Fun tab", () => MenuRoot.SelectTabByName("Fun"));
            Add("tab.console", "Open tab", "Console tab", () => MenuRoot.SelectTabByName("Console"));
            Add("tab.keybinds", "Open tab", "Keybinds tab", () => MenuRoot.SelectTabByName("Keybinds"));
        }
    }
}
