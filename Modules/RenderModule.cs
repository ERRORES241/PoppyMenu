using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace PoppyMenu
{
    internal class RenderModule : PoppyModule
    {
        internal override string Name => "ESP";

        internal static bool EspMobs;
        internal static bool EspInteractables;
        internal static bool EspTeleporter;

        internal static bool ShowNames = true;
        internal static bool ShowDistance = true;
        internal static bool ShowEnemyHealth = true;
        internal static bool ShowOutline = true;
        internal static float FontSize = 12f;
        internal static float MaxDistance;
        internal static float MarkerSize = 6f;

        internal static Color EnemyColor = Color.red;
        internal static Color InteractableColor = Color.cyan;
        internal static Color TeleporterColor = Color.yellow;

        private static GUIStyle _labelStyle;
        private static float _nextScan;

        private struct CachedInteractable
        {
            public Component Component;
            public Transform Transform;
            public string BaseName;
            public string ItemContent;
            public Color Color;
            public bool IsPurchase;
            public bool IsBarrel;
            public PurchaseInteraction Purchase;
            public BarrelInteraction Barrel;
        }

        private struct CachedPortal
        {
            public SceneExitController Controller;
            public Transform Transform;
            public string Label;
            public Color Color;
        }

        private static readonly List<CachedInteractable> _cachedInteractables = new List<CachedInteractable>();
        private static readonly List<CachedPortal> _cachedPortals = new List<CachedPortal>();

        internal override void Tick()
        {
            if (Time.realtimeSinceStartup >= _nextScan)
            {
                _nextScan = Time.realtimeSinceStartup + 2.0f;
                RebuildCache();
            }
        }

        private static void RebuildCache()
        {
            _cachedInteractables.Clear();
            _cachedPortals.Clear();

            if (!PlayerContext.InGame) return;

            if (EspInteractables)
            {
                var purchases = Object.FindObjectsOfType<PurchaseInteraction>();
                foreach (PurchaseInteraction pi in purchases)
                {
                    if (pi == null || !pi.available) continue;
                    CachedInteractable item = new CachedInteractable
                    {
                        Component = pi,
                        Transform = pi.transform,
                        Purchase = pi,
                        IsPurchase = true
                    };
                    GetInteractableStaticInfo(pi, out item.BaseName, out item.ItemContent, out item.Color);
                    _cachedInteractables.Add(item);
                }

                var barrels = Object.FindObjectsOfType<BarrelInteraction>();
                foreach (BarrelInteraction barrel in barrels)
                {
                    if (barrel == null || barrel.opened) continue;
                    _cachedInteractables.Add(new CachedInteractable
                    {
                        Component = barrel,
                        Transform = barrel.transform,
                        Barrel = barrel,
                        IsBarrel = true,
                        BaseName = "Barrel ($0)",
                        ItemContent = "",
                        Color = new Color(0.85f, 0.82f, 0.55f)
                    });
                }

                var plates = Object.FindObjectsOfType<PressurePlateController>();
                foreach (PressurePlateController plate in plates)
                {
                    if (plate == null) continue;
                    _cachedInteractables.Add(new CachedInteractable
                    {
                        Component = plate,
                        Transform = plate.transform,
                        BaseName = "Secret Button",
                        ItemContent = "",
                        Color = new Color(0.2f, 0.9f, 0.8f)
                    });
                }

                var timedChests = Object.FindObjectsOfType<TimedChestController>();
                foreach (TimedChestController timed in timedChests)
                {
                    if (timed == null) continue;
                    _cachedInteractables.Add(new CachedInteractable
                    {
                        Component = timed,
                        Transform = timed.transform,
                        BaseName = "Timed Chest",
                        ItemContent = "[Legendary]",
                        Color = new Color(0.9f, 0.22f, 0.29f)
                    });
                }
            }

            if (EspTeleporter)
            {
                var portals = Object.FindObjectsOfType<SceneExitController>();
                foreach (SceneExitController portal in portals)
                {
                    if (portal == null) continue;
                    CachedPortal cp = new CachedPortal
                    {
                        Controller = portal,
                        Transform = portal.transform
                    };
                    cp.Label = GetPortalStaticInfo(portal, out cp.Color);
                    _cachedPortals.Add(cp);
                }
            }
        }

        internal override void DrawMenu()
        {
            Widgets.SectionBegin("ESP / Wallhack");
            EspMobs = Widgets.Toggle("Enemies", EspMobs);
            EspInteractables = Widgets.Toggle("Interactables (Chests, Barrels, Pickups)", EspInteractables);
            EspTeleporter = Widgets.Toggle("Teleporter & Portals", EspTeleporter);
            Widgets.Hint("Markers draw through walls while in a run.");
            Widgets.SectionEnd();

            Widgets.SectionBegin("Display");
            ShowNames = Widgets.Toggle("Show names", ShowNames);
            ShowDistance = Widgets.Toggle("Show distance", ShowDistance);
            ShowEnemyHealth = Widgets.Toggle("Show enemy health", ShowEnemyHealth);
            ShowOutline = Widgets.Toggle("Text outline", ShowOutline);
            FontSize = Widgets.Slider("Font size", FontSize, 8f, 24f);
            MaxDistance = Widgets.Slider("Max distance (0 = unlimited)", MaxDistance, 0f, 500f);
            MarkerSize = Widgets.Slider("Marker size", MarkerSize, 2f, 16f);
            Widgets.SectionEnd();
        }

        internal override void DrawOverlay()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (!PlayerContext.InGame) return;
            if (!EspMobs && !EspInteractables && !EspTeleporter) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 origin = PlayerContext.HasBody ? PlayerContext.Body.corePosition : cam.transform.position;

            try
            {
                if (EspMobs)
                {
                    CharacterBody me = PlayerContext.Body;
                    TeamIndex myTeam = me != null && me.teamComponent != null ? me.teamComponent.teamIndex : TeamIndex.Player;

                    var bodies = CharacterBody.readOnlyInstancesList;
                    for (int i = 0; i < bodies.Count; i++)
                    {
                        CharacterBody body = bodies[i];
                        if (body == null || body == me) continue;
                        if (body.healthComponent == null || !body.healthComponent.alive) continue;
                        if (body.teamComponent == null) continue;

                        TeamIndex t = body.teamComponent.teamIndex;
                        if (t == myTeam || t == TeamIndex.None || t == TeamIndex.Neutral || t == TeamIndex.Player) continue;

                        if (Culled(origin, body.corePosition, out float dist)) continue;
                        DrawMarker(cam, body.corePosition, EnemyMultiLineLabel(body, dist), EnemyColor);
                    }
                }

                if (EspInteractables)
                {
                    for (int i = 0; i < _cachedInteractables.Count; i++)
                    {
                        CachedInteractable item = _cachedInteractables[i];
                        if (item.Component == null || item.Transform == null) continue;
                        if (item.IsPurchase && (item.Purchase == null || !item.Purchase.available)) continue;
                        if (item.IsBarrel && (item.Barrel == null || item.Barrel.opened)) continue;

                        if (Culled(origin, item.Transform.position, out float dist)) continue;

                        string bName = ShowNames ? item.BaseName : "";
                        string dStr = ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "";
                        string label = BuildMultiLine(bName, item.ItemContent, dStr);

                        DrawMarker(cam, item.Transform.position, label, item.Color);
                    }

                    var pickups = InstanceTracker.GetInstancesList<GenericPickupController>();
                    if (pickups != null)
                    {
                        for (int i = 0; i < pickups.Count; i++)
                        {
                            GenericPickupController gpc = pickups[i];
                            if (gpc == null || gpc.transform == null) continue;
                            if (Culled(origin, gpc.transform.position, out float dist)) continue;

                            string label = GetPickupMultiLineLabel(gpc, dist, out Color itemCol);
                            DrawMarker(cam, gpc.transform.position, label, itemCol);
                        }
                    }
                }

                if (EspTeleporter)
                {
                    if (TeleporterInteraction.instance != null)
                    {
                        Vector3 tp = TeleporterInteraction.instance.transform.position;
                        if (!Culled(origin, tp, out float dist))
                        {
                            string label = TeleporterMultiLineLabel(TeleporterInteraction.instance, dist);
                            DrawMarker(cam, tp, label, TeleporterColor);
                        }
                    }

                    for (int i = 0; i < _cachedPortals.Count; i++)
                    {
                        CachedPortal portal = _cachedPortals[i];
                        if (portal.Controller == null || portal.Transform == null) continue;
                        if (Culled(origin, portal.Transform.position, out float dist)) continue;

                        string dStr = ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "";
                        string pLabel = BuildMultiLine(ShowNames ? portal.Label : "", "", dStr);
                        DrawMarker(cam, portal.Transform.position, pLabel, portal.Color);
                    }
                }
            }
            catch { }
        }

        private static string GetPortalStaticInfo(SceneExitController sec, out Color color)
        {
            color = new Color(0.3f, 0.85f, 0.95f);
            string name = "Portal";

            if (sec != null)
            {
                if (sec.isColossusPortal)
                {
                    name = "Colossus Portal";
                    color = new Color(0.95f, 0.5f, 0.2f);
                }
                else if (sec.destinationScene != null)
                {
                    string sceneName = sec.destinationScene.baseSceneName;
                    if (sceneName == "bazaar") { name = "Blue Portal (Bazaar)"; color = new Color(0.25f, 0.65f, 1.0f); }
                    else if (sceneName == "goldshores") { name = "Gold Portal (Gilded Coast)"; color = new Color(1.0f, 0.85f, 0.2f); }
                    else if (sceneName == "mysteryspace") { name = "Celestial Portal"; color = new Color(0.85f, 0.95f, 1.0f); }
                    else if (sceneName == "voidstage" || sceneName == "voidraid") { name = "Void Portal"; color = new Color(0.8f, 0.25f, 0.95f); }
                    else name = $"{sec.destinationScene.baseSceneName} Portal";
                }
            }
            return name;
        }

        private static bool IsCommandArtifactActive()
        {
            try
            {
                return RunArtifactManager.instance != null && RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.Command);
            }
            catch
            {
                return false;
            }
        }

        private static void GetInteractableStaticInfo(PurchaseInteraction pi, out string baseName, out string contentStr, out Color overrideColor)
        {
            overrideColor = InteractableColor;
            baseName = "";
            contentStr = "";
            if (pi == null) return;

            bool isCloaked = (pi.name != null && (pi.name.IndexOf("Stealthed", System.StringComparison.OrdinalIgnoreCase) >= 0 || pi.name.IndexOf("Cloaked", System.StringComparison.OrdinalIgnoreCase) >= 0)) || pi.displayNameToken == "CHEST1STEALTHED_NAME";

            string costStr = "";
            if (pi.cost > 0)
            {
                if (pi.costType == CostTypeIndex.Money)
                    costStr = $" (${pi.cost})";
                else if (pi.costType == CostTypeIndex.LunarCoin)
                    costStr = $" ({pi.cost} Lunar)";
                else if (pi.costType == CostTypeIndex.PercentHealth)
                    costStr = $" ({pi.cost}% HP)";
                else
                    costStr = $" (${pi.cost})";
            }
            else if (isCloaked)
            {
                costStr = " ($0)";
            }

            baseName = isCloaked ? "Cloaked Chest" : pi.GetDisplayName();
            if (string.IsNullOrEmpty(baseName)) baseName = pi.name.Replace("(Clone)", "").Trim();
            if (!string.IsNullOrEmpty(baseName)) baseName += costStr;

            if (isCloaked)
            {
                overrideColor = new Color(0.95f, 0.35f, 0.95f);
            }

            PickupIndex pickupIndex = PickupIndex.none;
            ChestBehavior chest = pi.GetComponent<ChestBehavior>();
            if (chest != null)
            {
                pickupIndex = chest.dropPickup;
            }
            else
            {
                ShopTerminalBehavior terminal = pi.GetComponent<ShopTerminalBehavior>();
                if (terminal != null)
                {
                    pickupIndex = terminal.CurrentPickup().pickupIndex;
                }
            }

            if (pickupIndex != PickupIndex.none)
            {
                string contentName = GetPickupContentInfo(pickupIndex, out Color itemColor);
                if (itemColor != Color.clear && !isCloaked) overrideColor = itemColor;

                if (!string.IsNullOrEmpty(contentName))
                {
                    contentStr = $"[{contentName}]";
                }
            }
        }

        private static string GetPickupMultiLineLabel(GenericPickupController gpc, float dist, out Color overrideColor)
        {
            overrideColor = InteractableColor;
            if (gpc == null) return "";

            string contentName = GetPickupContentInfo(gpc.pickupIndex, out Color itemCol);
            if (itemCol != Color.clear) overrideColor = itemCol;

            string baseName = ShowNames ? (!string.IsNullOrEmpty(contentName) ? contentName : "Pickup") : "";
            string distStr = ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "";

            return BuildMultiLine(baseName, "", distStr);
        }

        private static string GetPickupContentInfo(PickupIndex pickupIndex, out Color contentColor)
        {
            contentColor = InteractableColor;
            if (pickupIndex == PickupIndex.none) return null;

            try
            {
                PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                if (pickupDef == null) return null;

                bool isCommand = IsCommandArtifactActive();
                contentColor = pickupDef.baseColor != Color.clear ? pickupDef.baseColor : InteractableColor;

                if (isCommand)
                {
                    return GetTierRarityName(pickupDef.itemTier);
                }
                else
                {
                    string name = Language.GetString(pickupDef.nameToken);
                    if (string.IsNullOrEmpty(name) || name == pickupDef.nameToken)
                        name = pickupDef.internalName;
                    return name;
                }
            }
            catch
            {
                return null;
            }
        }

        private static string GetTierRarityName(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Tier1: return "White (Common)";
                case ItemTier.Tier2: return "Green (Uncommon)";
                case ItemTier.Tier3: return "Red (Legendary)";
                case ItemTier.Boss: return "Yellow (Boss)";
                case ItemTier.Lunar: return "Blue (Lunar)";
                case ItemTier.VoidTier1:
                case ItemTier.VoidTier2:
                case ItemTier.VoidTier3:
                case ItemTier.VoidBoss: return "Purple (Void)";
                default: return "Command Essence";
            }
        }

        private static bool Culled(Vector3 origin, Vector3 pos, out float dist)
        {
            dist = Vector3.Distance(origin, pos);
            if (MaxDistance > 0f && dist > MaxDistance) return true;
            return false;
        }

        private static string EnemyMultiLineLabel(CharacterBody body, float dist)
        {
            string nameStr = ShowNames ? body.GetDisplayName() : "";
            string hpStr = (ShowEnemyHealth && body.healthComponent != null) ? $"{Mathf.CeilToInt(body.healthComponent.combinedHealth)} hp" : "";
            string distStr = ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "";

            return BuildMultiLine(nameStr, hpStr, distStr);
        }

        private static string TeleporterMultiLineLabel(TeleporterInteraction tp, float dist)
        {
            string nameStr = ShowNames ? "Teleporter" : "";
            string stateStr = tp.isCharged ? "(Charged)" : (tp.isCharging ? $"(Charging {Mathf.FloorToInt(tp.chargeFraction * 100)}%)" : "");
            string distStr = ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "";

            return BuildMultiLine(nameStr, stateStr, distStr);
        }

        private static string BuildMultiLine(string line1, string line2, string line3)
        {
            if (string.IsNullOrEmpty(line2) && string.IsNullOrEmpty(line3)) return line1;
            if (string.IsNullOrEmpty(line1) && string.IsNullOrEmpty(line3)) return line2;
            if (string.IsNullOrEmpty(line1) && string.IsNullOrEmpty(line2)) return line3;

            if (string.IsNullOrEmpty(line2)) return line1 + "\n" + line3;
            if (string.IsNullOrEmpty(line3)) return line1 + "\n" + line2;
            if (string.IsNullOrEmpty(line1)) return line2 + "\n" + line3;

            return line1 + "\n" + line2 + "\n" + line3;
        }

        private void DrawMarker(Camera cam, Vector3 worldPos, string label, Color color)
        {
            Vector3 sp = cam.WorldToScreenPoint(worldPos);
            if (sp.z <= 0f) return;
            float y = Screen.height - sp.y;
            float half = MarkerSize * 0.5f;

            Theme.Fill(new Rect(sp.x - half, y - half, MarkerSize, MarkerSize), color);

            if (string.IsNullOrEmpty(label)) return;

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperCenter,
                    fontStyle = FontStyle.Bold,
                    wordWrap = false
                };
            }

            int fSize = Mathf.Clamp(Mathf.RoundToInt(FontSize), 8, 30);
            _labelStyle.fontSize = fSize;

            GUIContent content = new GUIContent(label);
            float labelWidth = 240f;
            float labelHeight = _labelStyle.CalcHeight(content, labelWidth) + 8f;

            Rect labelRect = new Rect(sp.x - labelWidth * 0.5f, y + half + 2f, labelWidth, labelHeight);

            if (ShowOutline)
            {
                _labelStyle.normal.textColor = Color.black;
                GUI.Label(new Rect(labelRect.x - 1f, labelRect.y, labelRect.width, labelRect.height), content, _labelStyle);
                GUI.Label(new Rect(labelRect.x + 1f, labelRect.y, labelRect.width, labelRect.height), content, _labelStyle);
                GUI.Label(new Rect(labelRect.x, labelRect.y - 1f, labelRect.width, labelRect.height), content, _labelStyle);
                GUI.Label(new Rect(labelRect.x, labelRect.y + 1f, labelRect.width, labelRect.height), content, _labelStyle);
            }

            _labelStyle.normal.textColor = color;
            GUI.Label(labelRect, content, _labelStyle);
        }
    }
}
