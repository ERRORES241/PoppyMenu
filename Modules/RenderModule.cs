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
            MaxDistance = Widgets.Slider("Max distance (0 = unlimited)", MaxDistance, 0f, 500f);
            MarkerSize = Widgets.Slider("Marker size", MarkerSize, 2f, 16f);
            Widgets.SectionEnd();

            Widgets.SectionBegin("Colors");
            EnemyColor = ColorRow("Enemies", EnemyColor);
            InteractableColor = ColorRow("Interactables", InteractableColor);
            TeleporterColor = ColorRow("Teleporter", TeleporterColor);
            Widgets.SectionEnd();
        }

        private static Color ColorRow(string label, Color c)
        {
            Widgets.Label(label);
            c.r = Widgets.Slider("  R", c.r, 0f, 1f);
            c.g = Widgets.Slider("  G", c.g, 0f, 1f);
            c.b = Widgets.Slider("  B", c.b, 0f, 1f);
            return c;
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
                        DrawMarker(cam, body.corePosition, EnemyLabel(body, dist), EnemyColor);
                    }
                }

                if (EspInteractables)
                {
                    foreach (PurchaseInteraction pi in _interactables)
                    {
                        if (pi == null || !pi.available) continue;
                        if (Culled(origin, pi.transform.position, out float dist)) continue;

                        string name = GetInteractableLabel(pi, out Color itemCol);
                        DrawMarker(cam, pi.transform.position, Label(name, dist), itemCol);
                    }

                    var pickups = InstanceTracker.GetInstancesList<GenericPickupController>();
                    if (pickups != null)
                    {
                        foreach (GenericPickupController gpc in pickups)
                        {
                            if (gpc == null) continue;
                            if (Culled(origin, gpc.transform.position, out float dist)) continue;

                            string contentName = GetPickupContentInfo(gpc.pickupIndex, out Color itemCol);
                            string name = !string.IsNullOrEmpty(contentName) ? contentName : "Pickup";
                            DrawMarker(cam, gpc.transform.position, Label(name, dist), itemCol);
                        }
                    }
                }

                if (EspTeleporter && TeleporterInteraction.instance != null)
                {
                    Vector3 tp = TeleporterInteraction.instance.transform.position;
                    if (!Culled(origin, tp, out float dist))
                    {
                        string tpState = TeleporterInteraction.instance.isCharged ? "Teleporter (Charged)" : "Teleporter";
                        DrawMarker(cam, tp, Label(tpState, dist), TeleporterColor);
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

        private static string GetInteractableLabel(PurchaseInteraction pi, out Color overrideColor)
        {
            overrideColor = InteractableColor;
            if (pi == null) return "";

            string baseName = pi.GetDisplayName();
            if (string.IsNullOrEmpty(baseName)) baseName = pi.name.Replace("(Clone)", "").Trim();

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
                if (itemColor != Color.clear) overrideColor = itemColor;

                if (!string.IsNullOrEmpty(contentName))
                {
                    return $"{baseName} [{contentName}]";
                }
            }

            return baseName;
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

        private static string EnemyLabel(CharacterBody body, float dist)
        {
            string s = ShowNames ? body.GetDisplayName() : "";
            if (ShowEnemyHealth && body.healthComponent != null)
                s = Append(s, Mathf.CeilToInt(body.healthComponent.combinedHealth) + " hp");
            if (ShowDistance) s = Append(s, Mathf.RoundToInt(dist) + "m");
            return s;
        }

        private static string Label(string name, float dist)
        {
            string s = ShowNames ? name : "";
            if (ShowDistance) s = Append(s, Mathf.RoundToInt(dist) + "m");
            return s;
        }

        private static string Append(string a, string b) => a.Length > 0 ? a + "  " + b : b;

        private void DrawMarker(Camera cam, Vector3 worldPos, string label, Color color)
        {
            Vector3 sp = cam.WorldToScreenPoint(worldPos);
            if (sp.z <= 0f) return;
            float y = Screen.height - sp.y;
            float half = MarkerSize * 0.5f;

            Theme.Fill(new Rect(sp.x - half, y - half, MarkerSize, MarkerSize), color);

            if (string.IsNullOrEmpty(label)) return;
            if (_labelStyle == null) _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold };

            _labelStyle.normal.textColor = color;
            GUI.Label(new Rect(sp.x + half + 3f, y - 8f, 280f, 20f), label, _labelStyle);
        }
    }
}
