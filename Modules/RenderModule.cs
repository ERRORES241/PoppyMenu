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
        private static PurchaseInteraction[] _interactables = new PurchaseInteraction[0];
        private static float _nextScan;

        internal override void Tick()
        {
            if (EspInteractables && Time.realtimeSinceStartup >= _nextScan)
            {
                _interactables = Object.FindObjectsOfType<PurchaseInteraction>();
                _nextScan = Time.realtimeSinceStartup + 0.5f;
            }
        }

        internal override void DrawMenu()
        {
            Widgets.SectionBegin("ESP / Wallhack");
            EspMobs = Widgets.Toggle("Enemies", EspMobs);
            EspInteractables = Widgets.Toggle("Interactables", EspInteractables);
            EspTeleporter = Widgets.Toggle("Teleporter", EspTeleporter);
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

                    foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
                    {
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
                    foreach (PurchaseInteraction pi in _interactables)
                    {
                        if (pi == null || !pi.available) continue;
                        if (Culled(origin, pi.transform.position, out float dist)) continue;

                        string label = GetInteractableMultiLineLabel(pi, dist, out Color itemCol);
                        DrawMarker(cam, pi.transform.position, label, itemCol);
                    }

                    var pickups = InstanceTracker.GetInstancesList<GenericPickupController>();
                    if (pickups != null)
                    {
                        foreach (GenericPickupController gpc in pickups)
                        {
                            if (gpc == null) continue;
                            if (Culled(origin, gpc.transform.position, out float dist)) continue;

                            string label = GetPickupMultiLineLabel(gpc, dist, out Color itemCol);
                            DrawMarker(cam, gpc.transform.position, label, itemCol);
                        }
                    }
                }

                if (EspTeleporter && TeleporterInteraction.instance != null)
                {
                    Vector3 tp = TeleporterInteraction.instance.transform.position;
                    if (!Culled(origin, tp, out float dist))
                    {
                        string label = TeleporterMultiLineLabel(TeleporterInteraction.instance, dist);
                        DrawMarker(cam, tp, label, TeleporterColor);
                    }
                }
            }
            catch { }
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

        private static string GetInteractableMultiLineLabel(PurchaseInteraction pi, float dist, out Color overrideColor)
        {
            overrideColor = InteractableColor;
            if (pi == null) return "";

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

            string baseName = ShowNames ? pi.GetDisplayName() : "";
            if (string.IsNullOrEmpty(baseName) && ShowNames) baseName = pi.name.Replace("(Clone)", "").Trim();
            if (!string.IsNullOrEmpty(baseName)) baseName += costStr;

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

            string contentStr = "";
            if (pickupIndex != PickupIndex.none)
            {
                string contentName = GetPickupContentInfo(pickupIndex, out Color itemColor);
                if (itemColor != Color.clear) overrideColor = itemColor;

                if (!string.IsNullOrEmpty(contentName))
                {
                    contentStr = $"[{contentName}]";
                }
            }

            string distStr = ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "";

            return BuildMultiLine(baseName, contentStr, distStr);
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
                case ItemTier.VoidTier1: return "Void Common";
                case ItemTier.VoidTier2: return "Void Uncommon";
                case ItemTier.VoidTier3: return "Void Legendary";
                case ItemTier.VoidBoss: return "Void Boss";
                default: return tier.ToString();
            }
        }

        private static bool Culled(Vector3 origin, Vector3 target, out float dist)
        {
            dist = Vector3.Distance(origin, target);
            return MaxDistance > 0.5f && dist > MaxDistance;
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
            List<string> lines = new List<string>();
            if (!string.IsNullOrEmpty(line1)) lines.Add(line1);
            if (!string.IsNullOrEmpty(line2)) lines.Add(line2);
            if (!string.IsNullOrEmpty(line3)) lines.Add(line3);
            return string.Join("\n", lines);
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
                    fontStyle = FontStyle.Bold
                };
            }

            int fSize = Mathf.Clamp(Mathf.RoundToInt(FontSize), 8, 30);
            _labelStyle.fontSize = fSize;

            GUIContent content = new GUIContent(label);
            Vector2 size = _labelStyle.CalcSize(content);
            float labelWidth = Mathf.Max(size.x + 12f, 160f);
            float labelHeight = size.y;

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
