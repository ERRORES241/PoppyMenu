using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Newtonsoft.Json;
using UnityEngine;

namespace PoppyMenu
{
    internal class ConfigsModule : PoppyModule
    {
        internal override string Name => "Configs";

        private static string _newConfigName = "";
        private static string _pendingDelete;

        private static string ConfigsFolder => Path.Combine(Paths.ConfigPath, "PoppyMenuConfigs");

        private static void EnsureFolderExists()
        {
            if (!Directory.Exists(ConfigsFolder))
                Directory.CreateDirectory(ConfigsFolder);
        }

        internal static List<ConfigProfile> GetSavedProfiles()
        {
            EnsureFolderExists();
            List<ConfigProfile> list = new List<ConfigProfile>();
            string[] files = Directory.GetFiles(ConfigsFolder, "*.json");
            foreach (string file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    ConfigProfile profile = JsonConvert.DeserializeObject<ConfigProfile>(json);
                    if (profile != null)
                    {
                        if (string.IsNullOrEmpty(profile.Name))
                            profile.Name = Path.GetFileNameWithoutExtension(file);
                        list.Add(profile);
                    }
                }
                catch { }
            }
            return list;
        }

        internal static void SaveProfile(ConfigProfile profile)
        {
            EnsureFolderExists();
            if (profile == null || string.IsNullOrWhiteSpace(profile.Name)) return;

            string safeName = SanitizeFileName(profile.Name.Trim());
            string path = Path.Combine(ConfigsFolder, safeName + ".json");
            string json = JsonConvert.SerializeObject(profile, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        internal static void DeleteProfile(string name)
        {
            EnsureFolderExists();
            if (string.IsNullOrWhiteSpace(name)) return;
            string safeName = SanitizeFileName(name.Trim());
            string path = Path.Combine(ConfigsFolder, safeName + ".json");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        internal static void RenameProfile(ConfigProfile profile, string newName)
        {
            if (profile == null || string.IsNullOrWhiteSpace(newName)) return;
            DeleteProfile(profile.Name);
            profile.Name = newName.Trim();
            SaveProfile(profile);
        }

        internal static void SetDefaultStartup(ConfigProfile target)
        {
            List<ConfigProfile> profiles = GetSavedProfiles();
            foreach (ConfigProfile p in profiles)
            {
                p.IsDefaultStartup = (p.Name == target.Name && target.IsDefaultStartup);
                SaveProfile(p);
            }
        }

        internal static void ApplyStartupConfig()
        {
            List<ConfigProfile> profiles = GetSavedProfiles();
            foreach (ConfigProfile p in profiles)
            {
                if (p.IsDefaultStartup)
                {
                    p.Apply();
                    Notify.Push("Loaded startup config: " + p.Name);
                    break;
                }
            }
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }

        internal override void DrawMenu()
        {
            Widgets.SectionBegin("Configuration Manager");
            Widgets.Hint("Save, load, rename, or delete your custom feature & ESP profiles.");

            GUILayout.BeginHorizontal();
            _newConfigName = GUILayout.TextField(_newConfigName ?? "", Theme.Search);
            if (GUILayout.Button("+ Save Current", Theme.Primary, GUILayout.Width(130)))
            {
                string name = string.IsNullOrWhiteSpace(_newConfigName) ? "Config_" + DateTime.Now.ToString("HHmmss") : _newConfigName.Trim();
                ConfigProfile p = ConfigProfile.CaptureCurrent(name);
                SaveProfile(p);
                _newConfigName = "";
                Notify.Push("Saved config: " + p.Name);
            }
            GUILayout.EndHorizontal();
            Widgets.SectionEnd();

            List<ConfigProfile> profiles = GetSavedProfiles();
            if (profiles.Count == 0)
            {
                Widgets.SectionBegin("Saved Configs");
                Widgets.Label("No saved configs yet. Enter a name above and click '+ Save Current'.");
                Widgets.SectionEnd();
                return;
            }

            foreach (ConfigProfile p in profiles)
            {
                ConfigProfile profile = p;
                Widgets.SectionBegin(null);

                GUILayout.BeginHorizontal();
                string updatedName = GUILayout.TextField(profile.Name ?? "", Theme.Search);
                if (updatedName != profile.Name && !string.IsNullOrWhiteSpace(updatedName))
                {
                    RenameProfile(profile, updatedName);
                }

                if (GUILayout.Button("Load", Theme.Primary, GUILayout.Width(64)))
                {
                    profile.Apply();
                    Notify.Push("Loaded config: " + profile.Name);
                }

                if (GUILayout.Button("Overwrite", Theme.Button, GUILayout.Width(80)))
                {
                    ConfigProfile updated = ConfigProfile.CaptureCurrent(profile.Name);
                    updated.IsDefaultStartup = profile.IsDefaultStartup;
                    SaveProfile(updated);
                    Notify.Push("Overwrote config: " + profile.Name);
                }

                if (GUILayout.Button("Delete", Theme.Danger_, GUILayout.Width(64)))
                {
                    _pendingDelete = profile.Name;
                }
                GUILayout.EndHorizontal();

                bool isDefault = profile.IsDefaultStartup;
                bool newDefault = Widgets.Toggle("Load on startup (Default profile)", isDefault);
                if (newDefault != isDefault)
                {
                    profile.IsDefaultStartup = newDefault;
                    SetDefaultStartup(profile);
                }

                Widgets.SectionEnd();
            }

            if (!string.IsNullOrEmpty(_pendingDelete))
            {
                DeleteProfile(_pendingDelete);
                Notify.Push("Deleted config: " + _pendingDelete);
                _pendingDelete = null;
            }
        }
    }

    internal class ConfigProfile
    {
        public string Name { get; set; }
        public bool IsDefaultStartup { get; set; }

        public bool AimActive { get; set; }
        public bool AimTargetWeakPoints { get; set; }
        public bool AimNoSpread { get; set; }
        public bool AimNoRecoil { get; set; }
        public bool AimSticky { get; set; }
        public bool AimBossesOnly { get; set; }
        public bool AimCheckLos { get; set; }
        public bool AimMagicBullet { get; set; }
        public bool AimUseFov { get; set; }
        public bool AimDrawFov { get; set; }
        public float AimFovRadius { get; set; }
        public float AimMaxRange { get; set; }

        public bool EspMobs { get; set; }
        public bool EspInteractables { get; set; }
        public bool EspTeleporter { get; set; }
        public bool EspShowNames { get; set; }
        public bool EspShowDistance { get; set; }
        public bool EspShowHealth { get; set; }
        public bool EspShowOutline { get; set; }
        public float EspFontSize { get; set; }
        public float EspMaxDistance { get; set; }
        public float EspMarkerSize { get; set; }

        public bool MovementFlight { get; set; }
        public bool MovementNoclip { get; set; }
        public bool MovementAlwaysSprint { get; set; }
        public bool MovementJumpPack { get; set; }

        public bool GodMode { get; set; }
        public bool BuddhaMode { get; set; }
        public bool InfiniteSkills { get; set; }
        public bool NoEquipCooldown { get; set; }

        public bool StatDmgOn { get; set; }
        public float StatDmgMult { get; set; }
        public bool StatAtkOn { get; set; }
        public float StatAtkMult { get; set; }
        public bool StatMoveOn { get; set; }
        public float StatMoveMult { get; set; }
        public bool StatArmorOn { get; set; }
        public float StatArmorBonus { get; set; }
        public bool StatCritOn { get; set; }
        public float StatCritBonus { get; set; }
        public bool StatHpOn { get; set; }
        public float StatHpMult { get; set; }

        public static ConfigProfile CaptureCurrent(string name)
        {
            return new ConfigProfile
            {
                Name = name,
                AimActive = Aim.Enabled || Aim.Active,
                AimTargetWeakPoints = Aim.TargetWeakPoints,
                AimNoSpread = Aim.NoSpread,
                AimNoRecoil = Aim.NoRecoil,
                AimSticky = Aim.Sticky,
                AimBossesOnly = Aim.PrioritizeBosses,
                AimCheckLos = Aim.RequireLoS,
                AimMagicBullet = Aim.MagicBullet,
                AimUseFov = Aim.UseFov,
                AimDrawFov = Aim.ShowFovCircle,
                AimFovRadius = Aim.Fov,
                AimMaxRange = Aim.MaxRange,

                EspMobs = RenderModule.EspMobs,
                EspInteractables = RenderModule.EspInteractables,
                EspTeleporter = RenderModule.EspTeleporter,
                EspShowNames = RenderModule.ShowNames,
                EspShowDistance = RenderModule.ShowDistance,
                EspShowHealth = RenderModule.ShowEnemyHealth,
                EspShowOutline = RenderModule.ShowOutline,
                EspFontSize = RenderModule.FontSize,
                EspMaxDistance = RenderModule.MaxDistance,
                EspMarkerSize = RenderModule.MarkerSize,

                MovementFlight = MovementModule.Flight,
                MovementNoclip = MovementModule.NoClip,
                MovementAlwaysSprint = MovementModule.AlwaysSprint,
                MovementJumpPack = MovementModule.JumpPack,

                GodMode = PlayerModule.GodMode,
                BuddhaMode = Safety.Buddha,
                InfiniteSkills = PlayerModule.InfiniteSkills,
                NoEquipCooldown = ItemsModule.NoEquipmentCooldown,

                StatDmgOn = StatsModule.DamageOn,
                StatDmgMult = StatsModule.DamageMult,
                StatAtkOn = StatsModule.AttackSpeedOn,
                StatAtkMult = StatsModule.AttackSpeedMult,
                StatMoveOn = StatsModule.MoveSpeedOn,
                StatMoveMult = StatsModule.MoveSpeedMult,
                StatArmorOn = StatsModule.ArmorOn,
                StatArmorBonus = StatsModule.ArmorBonus,
                StatCritOn = StatsModule.CritOn,
                StatCritBonus = StatsModule.CritBonus,
                StatHpOn = StatsModule.MaxHealthOn,
                StatHpMult = StatsModule.MaxHealthMult
            };
        }

        public void Apply()
        {
            Aim.Enabled = AimActive;
            Aim.Active = AimActive;
            Aim.TargetWeakPoints = AimTargetWeakPoints;
            Aim.NoSpread = AimNoSpread;
            Aim.NoRecoil = AimNoRecoil;
            Aim.Sticky = AimSticky;
            Aim.PrioritizeBosses = AimBossesOnly;
            Aim.RequireLoS = AimCheckLos;
            Aim.MagicBullet = AimMagicBullet;
            Aim.UseFov = AimUseFov;
            Aim.ShowFovCircle = AimDrawFov;
            Aim.Fov = AimFovRadius;
            Aim.MaxRange = AimMaxRange;

            RenderModule.EspMobs = EspMobs;
            RenderModule.EspInteractables = EspInteractables;
            RenderModule.EspTeleporter = EspTeleporter;
            RenderModule.ShowNames = EspShowNames;
            RenderModule.ShowDistance = EspShowDistance;
            RenderModule.ShowEnemyHealth = EspShowHealth;
            RenderModule.ShowOutline = EspShowOutline;
            RenderModule.FontSize = EspFontSize;
            RenderModule.MaxDistance = EspMaxDistance;
            RenderModule.MarkerSize = EspMarkerSize;

            MovementModule.Flight = MovementFlight;
            MovementModule.NoClip = MovementNoclip;
            MovementModule.AlwaysSprint = MovementAlwaysSprint;
            MovementModule.JumpPack = MovementJumpPack;

            PlayerModule.GodMode = GodMode;
            Safety.Buddha = BuddhaMode;
            PlayerModule.InfiniteSkills = InfiniteSkills;
            ItemsModule.NoEquipmentCooldown = NoEquipCooldown;

            StatsModule.DamageOn = StatDmgOn;
            StatsModule.DamageMult = StatDmgMult;
            StatsModule.AttackSpeedOn = StatAtkOn;
            StatsModule.AttackSpeedMult = StatAtkMult;
            StatsModule.MoveSpeedOn = StatMoveOn;
            StatsModule.MoveSpeedMult = StatMoveMult;
            StatsModule.ArmorOn = StatArmorOn;
            StatsModule.ArmorBonus = StatArmorBonus;
            StatsModule.CritOn = StatCritOn;
            StatsModule.CritBonus = StatCritBonus;
            StatsModule.MaxHealthOn = StatHpOn;
            StatsModule.MaxHealthMult = StatHpMult;

            ModConfig.Save();
        }
    }
}
