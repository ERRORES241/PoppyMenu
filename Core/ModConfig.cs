using BepInEx.Configuration;
using UnityEngine;

namespace PoppyMenu
{
    internal static class ModConfig
    {
        private static ConfigFile _cfg;

        internal static ConfigEntry<KeyCode> ToggleMenuKey;
        internal static ConfigEntry<float> UiScale;
        internal static ConfigEntry<bool> RequireServerForCheats;
        internal static ConfigEntry<bool> AllowClientCheats;
        internal static ConfigEntry<bool> ShowHud;
        internal static ConfigEntry<float> WindowX;
        internal static ConfigEntry<float> WindowY;
        internal static ConfigEntry<float> WindowW;
        internal static ConfigEntry<float> WindowH;

        internal static ConfigEntry<KeyCode> SilentAimKey;

        internal static ConfigEntry<float> AccentR;
        internal static ConfigEntry<float> AccentG;
        internal static ConfigEntry<float> AccentB;

        internal static ConfigEntry<int> GiveMoneyAmount;
        internal static ConfigEntry<int> GiveXpAmount;
        internal static ConfigEntry<int> GiveCoinsAmount;
        internal static ConfigEntry<float> FlightSpeed;

        // Aimbot Configs
        internal static ConfigEntry<bool> AimbotActive;
        internal static ConfigEntry<bool> TargetWeakPoints;
        internal static ConfigEntry<bool> NoSpread;
        internal static ConfigEntry<bool> NoRecoil;
        internal static ConfigEntry<bool> StickyTarget;
        internal static ConfigEntry<bool> BossesOnly;
        internal static ConfigEntry<bool> CheckLos;
        internal static ConfigEntry<bool> MagicBullet;
        internal static ConfigEntry<bool> UseFov;
        internal static ConfigEntry<bool> DrawFov;
        internal static ConfigEntry<float> FovRadius;
        internal static ConfigEntry<float> MaxRange;

        // ESP Configs
        internal static ConfigEntry<bool> EspMobs;
        internal static ConfigEntry<bool> EspInteractables;
        internal static ConfigEntry<bool> EspTeleporter;
        internal static ConfigEntry<bool> EspShowNames;
        internal static ConfigEntry<bool> EspShowDistance;
        internal static ConfigEntry<bool> EspShowHealth;
        internal static ConfigEntry<bool> EspShowOutline;
        internal static ConfigEntry<float> EspFontSize;
        internal static ConfigEntry<float> EspMaxDistance;
        internal static ConfigEntry<float> EspMarkerSize;
        internal static ConfigEntry<float> EspEnemyColorR, EspEnemyColorG, EspEnemyColorB;
        internal static ConfigEntry<float> EspInteractableColorR, EspInteractableColorG, EspInteractableColorB;
        internal static ConfigEntry<float> EspTeleporterColorR, EspTeleporterColorG, EspTeleporterColorB;

        // Movement Configs
        internal static ConfigEntry<bool> MovementFlight;
        internal static ConfigEntry<bool> MovementNoclip;
        internal static ConfigEntry<bool> MovementAlwaysSprint;
        internal static ConfigEntry<bool> MovementJumpPack;

        // Combat Configs
        internal static ConfigEntry<bool> GodMode;
        internal static ConfigEntry<bool> BuddhaMode;
        internal static ConfigEntry<bool> InfiniteSkills;
        internal static ConfigEntry<bool> NoEquipCooldown;

        // Stats Configs
        internal static ConfigEntry<bool> StatDmgOn;
        internal static ConfigEntry<float> StatDmgMult;
        internal static ConfigEntry<bool> StatAtkOn;
        internal static ConfigEntry<float> StatAtkMult;
        internal static ConfigEntry<bool> StatMoveOn;
        internal static ConfigEntry<float> StatMoveMult;
        internal static ConfigEntry<bool> StatArmorOn;
        internal static ConfigEntry<float> StatArmorBonus;
        internal static ConfigEntry<bool> StatCritOn;
        internal static ConfigEntry<float> StatCritBonus;
        internal static ConfigEntry<bool> StatHpOn;
        internal static ConfigEntry<bool> GuaranteedShrineChance;
        internal static ConfigEntry<float> StatHpMult;

        internal static void Init(ConfigFile cfg)
        {
            _cfg = cfg;

            ToggleMenuKey = cfg.Bind("General", "ToggleMenuKey", KeyCode.Insert, "Opens/closes the Poppy menu.");
            UiScale = cfg.Bind("General", "UiScale", 1.0f, new ConfigDescription("Menu scale.", new AcceptableValueRange<float>(0.6f, 2.0f)));
            RequireServerForCheats = cfg.Bind("General", "RequireServerForCheats", false, "When true, server-side actions are skipped on non-host clients.");
            AllowClientCheats = cfg.Bind("General", "AllowClientCheats", false, "HOST ONLY: allow other clients to use this menu.");
            ShowHud = cfg.Bind("General", "ShowActiveEffectsHud", true, "Show a small active-effects HUD when the menu is closed.");
            WindowX = cfg.Bind("General", "WindowX", 40f, "Remembered menu window X position.");
            WindowY = cfg.Bind("General", "WindowY", 60f, "Remembered menu window Y position.");
            WindowW = cfg.Bind("General", "WindowW", 540f, "Remembered menu window width.");
            WindowH = cfg.Bind("General", "WindowH", 600f, "Remembered menu window height.");

            SilentAimKey = cfg.Bind("Hotkeys", "SilentAimHoldKey", KeyCode.None, "Hold-to-aim key.");

            AccentR = cfg.Bind("Theme", "AccentR", 0.898f, "Menu accent color, red channel.");
            AccentG = cfg.Bind("Theme", "AccentG", 0.219f, "Menu accent color, green channel.");
            AccentB = cfg.Bind("Theme", "AccentB", 0.290f, "Menu accent color, blue channel.");
            Theme.ApplyAccent(new Color(AccentR.Value, AccentG.Value, AccentB.Value));

            GiveMoneyAmount = cfg.Bind("Tunables", "GiveMoneyAmount", 1000, "Gold granted per click.");
            GiveXpAmount = cfg.Bind("Tunables", "GiveXpAmount", 100, "XP granted per click.");
            GiveCoinsAmount = cfg.Bind("Tunables", "GiveCoinsAmount", 10, "Lunar coins granted per click.");
            FlightSpeed = cfg.Bind("Tunables", "FlightSpeed", 40f, "Flight movement speed.");

            // Aimbot Binds
            AimbotActive = cfg.Bind("Aimbot", "Active", false, "Aimbot master toggle.");
            TargetWeakPoints = cfg.Bind("Aimbot", "TargetWeakPoints", true, "Railgunner weak point auto-targeting.");
            NoSpread = cfg.Bind("Aimbot", "NoSpread", false, "Disable weapon bullet spread.");
            NoRecoil = cfg.Bind("Aimbot", "NoRecoil", false, "Disable camera recoil.");
            StickyTarget = cfg.Bind("Aimbot", "StickyTarget", false, "Keep locked target until dead or out of range.");
            BossesOnly = cfg.Bind("Aimbot", "BossesOnly", false, "Target bosses only.");
            CheckLos = cfg.Bind("Aimbot", "CheckLos", true, "Check line of sight.");
            MagicBullet = cfg.Bind("Aimbot", "MagicBullet", false, "Shots pass through terrain.");
            UseFov = cfg.Bind("Aimbot", "UseFov", false, "Limit aimbot to FOV cone.");
            DrawFov = cfg.Bind("Aimbot", "DrawFov", false, "Draw FOV circle.");
            FovRadius = cfg.Bind("Aimbot", "FovRadius", 150f, "FOV circle radius.");
            MaxRange = cfg.Bind("Aimbot", "MaxRange", 400f, "Aimbot max range.");

            // ESP Binds
            EspMobs = cfg.Bind("ESP", "EspMobs", false, "Show enemy ESP.");
            EspInteractables = cfg.Bind("ESP", "EspInteractables", false, "Show chest & interactables ESP.");
            EspTeleporter = cfg.Bind("ESP", "EspTeleporter", false, "Show teleporter ESP.");
            EspShowNames = cfg.Bind("ESP", "ShowNames", true, "Show names on ESP.");
            EspShowDistance = cfg.Bind("ESP", "ShowDistance", true, "Show distance on ESP.");
            EspShowHealth = cfg.Bind("ESP", "ShowHealth", true, "Show enemy HP on ESP.");
            EspShowOutline = cfg.Bind("ESP", "ShowOutline", true, "Text outline on ESP.");
            EspFontSize = cfg.Bind("ESP", "FontSize", 12f, "Font size for ESP.");
            EspMaxDistance = cfg.Bind("ESP", "MaxDistance", 0f, "Max ESP distance.");
            EspMarkerSize = cfg.Bind("ESP", "MarkerSize", 6f, "Marker size.");
            EspEnemyColorR = cfg.Bind("ESP", "EnemyColorR", 1f, "Enemy color R.");
            EspEnemyColorG = cfg.Bind("ESP", "EnemyColorG", 0f, "Enemy color G.");
            EspEnemyColorB = cfg.Bind("ESP", "EnemyColorB", 0f, "Enemy color B.");
            EspInteractableColorR = cfg.Bind("ESP", "InteractableColorR", 0f, "Interactable color R.");
            EspInteractableColorG = cfg.Bind("ESP", "InteractableColorG", 1f, "Interactable color G.");
            EspInteractableColorB = cfg.Bind("ESP", "InteractableColorB", 1f, "Interactable color B.");
            EspTeleporterColorR = cfg.Bind("ESP", "TeleporterColorR", 1f, "Teleporter color R.");
            EspTeleporterColorG = cfg.Bind("ESP", "TeleporterColorG", 0.92f, "Teleporter color G.");
            EspTeleporterColorB = cfg.Bind("ESP", "TeleporterColorB", 0.016f, "Teleporter color B.");

            // Movement Binds
            MovementFlight = cfg.Bind("Movement", "Flight", false, "Flight toggle.");
            MovementNoclip = cfg.Bind("Movement", "Noclip", false, "Noclip toggle.");
            MovementAlwaysSprint = cfg.Bind("Movement", "AlwaysSprint", false, "Always sprint toggle.");
            MovementJumpPack = cfg.Bind("Movement", "JumpPack", false, "Jump pack toggle.");

            // Combat Binds
            GodMode = cfg.Bind("Combat", "GodMode", false, "God mode toggle.");
            BuddhaMode = cfg.Bind("Combat", "BuddhaMode", false, "Buddha mode toggle.");
            InfiniteSkills = cfg.Bind("Combat", "InfiniteSkills", false, "Infinite skills toggle.");
            NoEquipCooldown = cfg.Bind("Combat", "NoEquipCooldown", false, "No equipment cooldown toggle.");

            // Stats Binds
            StatDmgOn = cfg.Bind("Stats", "DamageOn", false, "Damage multiplier toggle.");
            StatDmgMult = cfg.Bind("Stats", "DamageMult", 1f, "Damage multiplier.");
            StatAtkOn = cfg.Bind("Stats", "AttackSpeedOn", false, "Attack speed multiplier toggle.");
            StatAtkMult = cfg.Bind("Stats", "AttackSpeedMult", 1f, "Attack speed multiplier.");
            StatMoveOn = cfg.Bind("Stats", "MoveSpeedOn", false, "Move speed multiplier toggle.");
            StatMoveMult = cfg.Bind("Stats", "MoveSpeedMult", 1f, "Move speed multiplier.");
            StatArmorOn = cfg.Bind("Stats", "ArmorOn", false, "Armor bonus toggle.");
            StatArmorBonus = cfg.Bind("Stats", "ArmorBonus", 0f, "Armor bonus.");
            StatCritOn = cfg.Bind("Stats", "CritOn", false, "Crit chance bonus toggle.");
            StatCritBonus = cfg.Bind("Stats", "CritBonus", 100f, "Crit chance bonus (+%).");
            StatHpOn = cfg.Bind("Stats", "MaxHealthOn", false, "Max health multiplier toggle.");
            StatHpMult = cfg.Bind("Stats", "MaxHealthMult", 1f, "Max health multiplier.");
            GuaranteedShrineChance = cfg.Bind("World", "GuaranteedShrineChance", false, "100% success rate on Shrine of Chance.");

            LoadFromConfig();
        }

        internal static void LoadFromConfig()
        {
            Aim.Enabled = AimbotActive.Value;
            Aim.Active = AimbotActive.Value;
            Aim.TargetWeakPoints = TargetWeakPoints.Value;
            Aim.NoSpread = NoSpread.Value;
            Aim.NoRecoil = NoRecoil.Value;
            Aim.Sticky = StickyTarget.Value;
            Aim.PrioritizeBosses = BossesOnly.Value;
            Aim.RequireLoS = CheckLos.Value;
            Aim.MagicBullet = MagicBullet.Value;
            Aim.UseFov = UseFov.Value;
            Aim.ShowFovCircle = DrawFov.Value;
            Aim.Fov = FovRadius.Value;
            Aim.MaxRange = MaxRange.Value;

            RenderModule.EspMobs = EspMobs.Value;
            RenderModule.EspInteractables = EspInteractables.Value;
            RenderModule.EspTeleporter = EspTeleporter.Value;
            RenderModule.ShowNames = EspShowNames.Value;
            RenderModule.ShowDistance = EspShowDistance.Value;
            RenderModule.ShowEnemyHealth = EspShowHealth.Value;
            RenderModule.ShowOutline = EspShowOutline.Value;
            RenderModule.FontSize = EspFontSize.Value;
            RenderModule.MaxDistance = EspMaxDistance.Value;
            RenderModule.MarkerSize = EspMarkerSize.Value;
            RenderModule.EnemyColor = new Color(EspEnemyColorR.Value, EspEnemyColorG.Value, EspEnemyColorB.Value);
            RenderModule.InteractableColor = new Color(EspInteractableColorR.Value, EspInteractableColorG.Value, EspInteractableColorB.Value);
            RenderModule.TeleporterColor = new Color(EspTeleporterColorR.Value, EspTeleporterColorG.Value, EspTeleporterColorB.Value);

            MovementModule.Flight = MovementFlight.Value;
            MovementModule.NoClip = MovementNoclip.Value;
            MovementModule.AlwaysSprint = MovementAlwaysSprint.Value;
            MovementModule.JumpPack = MovementJumpPack.Value;

            PlayerModule.GodMode = GodMode.Value;
            Safety.Buddha = BuddhaMode.Value;
            PlayerModule.InfiniteSkills = InfiniteSkills.Value;
            ItemsModule.NoEquipmentCooldown = NoEquipCooldown.Value;

            StatsModule.DamageOn = StatDmgOn.Value;
            StatsModule.DamageMult = StatDmgMult.Value;
            StatsModule.AttackSpeedOn = StatAtkOn.Value;
            StatsModule.AttackSpeedMult = StatAtkMult.Value;
            StatsModule.MoveSpeedOn = StatMoveOn.Value;
            StatsModule.MoveSpeedMult = StatMoveMult.Value;
            StatsModule.ArmorOn = StatArmorOn.Value;
            StatsModule.ArmorBonus = StatArmorBonus.Value;
            StatsModule.CritOn = StatCritOn.Value;
            StatsModule.CritBonus = StatCritBonus.Value;
            StatsModule.MaxHealthOn = StatHpOn.Value;
            StatsModule.MaxHealthMult = StatHpMult.Value;
            WorldModule.GuaranteedShrineChance = GuaranteedShrineChance.Value;
        }

        internal static void Save()
        {
            if (_cfg == null) return;

            AimbotActive.Value = Aim.Enabled || Aim.Active;
            TargetWeakPoints.Value = Aim.TargetWeakPoints;
            NoSpread.Value = Aim.NoSpread;
            NoRecoil.Value = Aim.NoRecoil;
            StickyTarget.Value = Aim.Sticky;
            BossesOnly.Value = Aim.PrioritizeBosses;
            CheckLos.Value = Aim.RequireLoS;
            MagicBullet.Value = Aim.MagicBullet;
            UseFov.Value = Aim.UseFov;
            DrawFov.Value = Aim.ShowFovCircle;
            FovRadius.Value = Aim.Fov;
            MaxRange.Value = Aim.MaxRange;

            EspMobs.Value = RenderModule.EspMobs;
            EspInteractables.Value = RenderModule.EspInteractables;
            EspTeleporter.Value = RenderModule.EspTeleporter;
            EspShowNames.Value = RenderModule.ShowNames;
            EspShowDistance.Value = RenderModule.ShowDistance;
            EspShowHealth.Value = RenderModule.ShowEnemyHealth;
            EspShowOutline.Value = RenderModule.ShowOutline;
            EspFontSize.Value = RenderModule.FontSize;
            EspMaxDistance.Value = RenderModule.MaxDistance;
            EspMarkerSize.Value = RenderModule.MarkerSize;
            EspEnemyColorR.Value = RenderModule.EnemyColor.r;
            EspEnemyColorG.Value = RenderModule.EnemyColor.g;
            EspEnemyColorB.Value = RenderModule.EnemyColor.b;
            EspInteractableColorR.Value = RenderModule.InteractableColor.r;
            EspInteractableColorG.Value = RenderModule.InteractableColor.g;
            EspInteractableColorB.Value = RenderModule.InteractableColor.b;
            EspTeleporterColorR.Value = RenderModule.TeleporterColor.r;
            EspTeleporterColorG.Value = RenderModule.TeleporterColor.g;
            EspTeleporterColorB.Value = RenderModule.TeleporterColor.b;

            MovementFlight.Value = MovementModule.Flight;
            MovementNoclip.Value = MovementModule.NoClip;
            MovementAlwaysSprint.Value = MovementModule.AlwaysSprint;
            MovementJumpPack.Value = MovementModule.JumpPack;

            GodMode.Value = PlayerModule.GodMode;
            BuddhaMode.Value = Safety.Buddha;
            InfiniteSkills.Value = PlayerModule.InfiniteSkills;
            NoEquipCooldown.Value = ItemsModule.NoEquipmentCooldown;

            StatDmgOn.Value = StatsModule.DamageOn;
            StatDmgMult.Value = StatsModule.DamageMult;
            StatAtkOn.Value = StatsModule.AttackSpeedOn;
            StatAtkMult.Value = StatsModule.AttackSpeedMult;
            StatMoveOn.Value = StatsModule.MoveSpeedOn;
            StatMoveMult.Value = StatsModule.MoveSpeedMult;
            StatArmorOn.Value = StatsModule.ArmorOn;
            StatArmorBonus.Value = StatsModule.ArmorBonus;
            StatCritOn.Value = StatsModule.CritOn;
            StatCritBonus.Value = StatsModule.CritBonus;
            StatHpOn.Value = StatsModule.MaxHealthOn;
            StatHpMult.Value = StatsModule.MaxHealthMult;
            GuaranteedShrineChance.Value = WorldModule.GuaranteedShrineChance;

            _cfg.Save();
        }
    }
}
