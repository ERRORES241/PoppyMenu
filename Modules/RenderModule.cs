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

        internal static bool FovOverride;
        internal static float FovValue = 90f;

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
        private static PressurePlateController[] _plates = new PressurePlateController[0];
        private static ShrineCleanseBehavior[] _cleansePools = new ShrineCleanseBehavior[0];
        private static SceneDef _lastStageScene;

        internal override void Tick()
        {
            if (!PlayerContext.InGame)
            {
                _lastStageScene = null;
                return;
            }

            // ── Camera FOV Changer ──
            if (FovOverride)
            {
                foreach (var cameraRig in CameraRigController.readOnlyInstancesList)
                {
                    if (cameraRig != null)
                        cameraRig.baseFov = FovValue;
                }
            }

            SceneDef currentScene = Stage.instance != null ? Stage.instance.sceneDef : null;
            if (currentScene != _lastStageScene)
            {
                _lastStageScene = currentScene;
                RefreshStageObjects();
            }
        }

        internal static void RefreshStageObjects()
        {
            try
            {
                _plates = Object.FindObjectsOfType<PressurePlateController>();
                _cleansePools = Object.FindObjectsOfType<ShrineCleanseBehavior>();
            }
            catch { }
        }

        internal override void DrawMenu()
        {
            Widgets.SectionBegin("HUD");
            ModConfig.ShowHud.Value = Widgets.Toggle("Active Effects HUD", ModConfig.ShowHud.Value);
            Widgets.SectionEnd();

            Widgets.SectionBegin("Camera & FOV");
            FovOverride = Widgets.Toggle("FOV Changer", FovOverride);
            if (FovOverride)
            {
                FovValue = Widgets.Slider("Field of View", FovValue, 60f, 140f);
            }
            Widgets.SectionEnd();

            Widgets.SectionBegin("ESP / Wallhack");
            EspMobs = Widgets.Toggle("Enemies", EspMobs);
            EspInteractables = Widgets.Toggle("Interactables (Chests, Barrels, Pickups)", EspInteractables);
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
                    // 1. PurchaseInteractions (Chests, Multishops, Printers, Shrines, Lunar Pods) via InstanceTracker
                    var purchases = InstanceTracker.GetInstancesList<PurchaseInteraction>();
                    if (purchases != null)
                    {
                        for (int i = 0; i < purchases.Count; i++)
                        {
                            PurchaseInteraction pi = purchases[i];
                            if (pi == null || !pi.available || pi.transform == null) continue;
                            if (Culled(origin, pi.transform.position, out float dist)) continue;

                            string label = GetInteractableMultiLineLabel(pi, dist, out Color itemCol);
                            DrawMarker(cam, pi.transform.position, label, itemCol);
                        }
                    }

                    // 2. Barrels via InstanceTracker
                    var barrels = InstanceTracker.GetInstancesList<BarrelInteraction>();
                    if (barrels != null)
                    {
                        for (int i = 0; i < barrels.Count; i++)
                        {
                            BarrelInteraction barrel = barrels[i];
                            if (barrel == null || barrel.opened || barrel.transform == null) continue;
                            if (Culled(origin, barrel.transform.position, out float dist)) continue;

                            string nameStr = barrel.GetDisplayName();
                            if (string.IsNullOrEmpty(nameStr)) nameStr = "Barrel";
                            string bName = ShowNames ? $"{nameStr} ($0)" : "";

                            Color col = new Color(0.85f, 0.82f, 0.55f);
                            if (barrel.name != null && barrel.name.IndexOf("Void", System.StringComparison.OrdinalIgnoreCase) >= 0)
                                col = new Color(0.75f, 0.35f, 0.9f);

                            string dStr = ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "";
                            string label = BuildMultiLine(bName, "", dStr);
                            DrawMarker(cam, barrel.transform.position, label, col);
                        }
                    }

                    // 3. ChestBehaviors (Timed Chests, etc.) via InstanceTracker
                    var chests = InstanceTracker.GetInstancesList<ChestBehavior>();
                    if (chests != null)
                    {
                        for (int i = 0; i < chests.Count; i++)
                        {
                            ChestBehavior chest = chests[i];
                            if (chest == null || chest.transform == null) continue;
                            TimedChestController timed = chest.GetComponent<TimedChestController>();
                            if (timed == null) continue;
                            if (Culled(origin, timed.transform.position, out float dist)) continue;

                            string tName = ShowNames ? "Timed Chest" : "";
                            string dStr = ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "";
                            string label = BuildMultiLine(tName, "[Legendary]", dStr);
                            DrawMarker(cam, timed.transform.position, label, new Color(0.9f, 0.22f, 0.29f));
                        }
                    }

                    // 4. Secret Pressure Plates
                    for (int i = 0; i < _plates.Length; i++)
                    {
                        PressurePlateController plate = _plates[i];
                        if (plate == null || plate.transform == null) continue;
                        if (Culled(origin, plate.transform.position, out float dist)) continue;

                        string pName = ShowNames ? "Secret Button" : "";
                        string dStr = ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "";
                        string label = BuildMultiLine(pName, "", dStr);
                        DrawMarker(cam, plate.transform.position, label, new Color(0.2f, 0.9f, 0.8f));
                    }

                    // 5. Pickups via InstanceTracker
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

                    // 6. PickupPickerControllers (Scrappers, Void Potentials, Command Cubes)
                    var pickers = InstanceTracker.GetInstancesList<PickupPickerController>();
                    if (pickers != null)
                    {
                        for (int i = 0; i < pickers.Count; i++)
                        {
                            PickupPickerController picker = pickers[i];
                            if (picker == null || !picker.available || picker.transform == null) continue;
                            
                            // Skip if attached to GenericPickupController (some Command Cubes might have it, but usually they don't)
                            if (picker.GetComponent<GenericPickupController>() != null) continue;
                            if (Culled(origin, picker.transform.position, out float dist)) continue;

                            string objName = picker.gameObject.name;
                            string pName = picker.GetContextString(null);
                            Color pColor = new Color(0.85f, 0.4f, 0.1f); // Default Orange

                            if (objName.IndexOf("CommandCube", System.StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                PickupIndexNetworker networker = picker.GetComponent<PickupIndexNetworker>();
                                if (networker != null)
                                {
                                    string contentName = GetPickupContentInfo(networker.NetworkpickupState.pickupIndex, out Color itemCol, isInteractable: false, isUnchosenCommandCube: true);
                                    if (itemCol != Color.clear) pColor = itemCol;
                                    pName = ShowNames ? (!string.IsNullOrEmpty(contentName) ? contentName : "Command Cube") : "";
                                }
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(pName))
                                {
                                    if (objName.IndexOf("Scrapper", System.StringComparison.OrdinalIgnoreCase) >= 0) pName = "Scrapper";
                                    else if (objName.IndexOf("Void", System.StringComparison.OrdinalIgnoreCase) >= 0 || objName.IndexOf("OptionPickup", System.StringComparison.OrdinalIgnoreCase) >= 0) pName = "Void Potential";
                                    else pName = objName.Replace("(Clone)", "").Trim();
                                }
                                else if (!ShowNames)
                                {
                                    pName = "";
                                }

                                if (objName.IndexOf("Void", System.StringComparison.OrdinalIgnoreCase) >= 0 || objName.IndexOf("OptionPickup", System.StringComparison.OrdinalIgnoreCase) >= 0)
                                    pColor = new Color(0.8f, 0.3f, 0.8f); // Purple for Void Potentials
                            }

                            string label = BuildMultiLine(pName, "", ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "");
                            DrawMarker(cam, picker.transform.position, label, pColor);
                        }
                    }

                    // 7. Cleansing Pools (Not in InstanceTracker)
                    if (_cleansePools != null)
                    {
                        for (int i = 0; i < _cleansePools.Length; i++)
                        {
                            ShrineCleanseBehavior pool = _cleansePools[i];
                            if (pool == null || pool.transform == null) continue;
                            if (Culled(origin, pool.transform.position, out float dist)) continue;

                            string pName = ShowNames ? Language.GetString(pool.contextToken) : "";
                            if (string.IsNullOrEmpty(pName) || pName == pool.contextToken) pName = "Cleansing Pool";
                            
                            string label = BuildMultiLine(pName, "", ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "");
                            DrawMarker(cam, pool.transform.position, label, new Color(0.9f, 0.9f, 0.9f));
                        }
                    }

                    // 8. Bazaar Upgrade Interactions (Lunar Seers / Soup Cauldrons)
                    var bazaarUpgrades = InstanceTracker.GetInstancesList<BazaarUpgradeInteraction>();
                    if (bazaarUpgrades != null)
                    {
                        for (int i = 0; i < bazaarUpgrades.Count; i++)
                        {
                            var upgrade = bazaarUpgrades[i];
                            if (upgrade == null || !upgrade.available || upgrade.transform == null) continue;
                            if (Culled(origin, upgrade.transform.position, out float dist)) continue;

                            string pName = ShowNames ? Language.GetString(upgrade.displayNameToken) : "";
                            if (string.IsNullOrEmpty(pName) || pName == upgrade.displayNameToken) pName = "Bazaar Upgrade";
                            
                            string label = BuildMultiLine(pName, "", ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "");
                            DrawMarker(cam, upgrade.transform.position, label, new Color(0.3f, 0.5f, 0.9f));
                        }
                    }

                    // 9. Geode / Void Seed
                    var geodes = InstanceTracker.GetInstancesList<GeodeController>();
                    if (geodes != null)
                    {
                        for (int i = 0; i < geodes.Count; i++)
                        {
                            var geode = geodes[i];
                            if (geode == null || !geode.Networkavailable || geode.transform == null) continue;
                            if (Culled(origin, geode.transform.position, out float dist)) continue;

                            string pName = ShowNames ? geode.GetDisplayName() : "";
                            if (string.IsNullOrEmpty(pName) || pName == geode.displayNameToken) pName = "Void Seed / Geode";

                            string label = BuildMultiLine(pName, "", ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "");
                            DrawMarker(cam, geode.transform.position, label, new Color(0.7f, 0.2f, 0.7f));
                        }
                    }

                    // 10. Drone Combiner (Col. Droneman)
                    var combiners = InstanceTracker.GetInstancesList<DroneCombinerController>();
                    if (combiners != null)
                    {
                        for (int i = 0; i < combiners.Count; i++)
                        {
                            var combiner = combiners[i];
                            if (combiner == null || combiner.transform == null) continue;
                            if (Culled(origin, combiner.transform.position, out float dist)) continue;

                            string pName = ShowNames ? combiner.GetContextString(null) : "";
                            if (string.IsNullOrEmpty(pName)) pName = "Drone Combiner";

                            string label = BuildMultiLine(pName, "", ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "");
                            DrawMarker(cam, combiner.transform.position, label, new Color(0.8f, 0.8f, 0.8f));
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

            if (pi.gameObject.name.IndexOf("Void", System.StringComparison.OrdinalIgnoreCase) >= 0)
                overrideColor = new Color(0.8f, 0.3f, 0.8f); // Purple for Void Cradles
            else if (pi.gameObject.name.IndexOf("Blood", System.StringComparison.OrdinalIgnoreCase) >= 0)
                overrideColor = new Color(0.8f, 0.2f, 0.2f); // Red for Blood Shrines
            else if (pi.costType == CostTypeIndex.LunarCoin)
                overrideColor = new Color(0.4f, 0.6f, 0.9f); // Blueish for Lunar interactions

            bool isCloaked = (pi.gameObject.name.IndexOf("Stealthed", System.StringComparison.OrdinalIgnoreCase) >= 0 || pi.gameObject.name.IndexOf("Cloaked", System.StringComparison.OrdinalIgnoreCase) >= 0) || pi.displayNameToken == "CHEST1STEALTHED_NAME";

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

            string baseName = ShowNames ? (isCloaked ? "Cloaked Chest" : pi.GetDisplayName()) : "";
            if (string.IsNullOrEmpty(baseName) && ShowNames) baseName = pi.name.Replace("(Clone)", "").Trim();
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

            string contentStr = "";
            if (pickupIndex != PickupIndex.none)
            {
                string contentName = GetPickupContentInfo(pickupIndex, out Color itemColor, isInteractable: true);
                if (itemColor != Color.clear && !isCloaked) overrideColor = itemColor;

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

            bool isVoidPotential = gpc.gameObject.name.IndexOf("OptionPickup", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (isVoidPotential)
            {
                overrideColor = new Color(0.8f, 0.3f, 0.8f); // Purple
                string pName = "Void Potential";
                PickupPickerController picker = gpc.GetComponent<PickupPickerController>();
                if (picker != null)
                {
                    string contextStr = picker.GetContextString(null);
                    if (!string.IsNullOrEmpty(contextStr)) pName = contextStr;
                }
                return BuildMultiLine(ShowNames ? pName : "", "", ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "");
            }

            bool isUnchosenCommand = IsCommandArtifactActive() && (gpc.GetComponent<PickupPickerController>() != null);
            string contentName = GetPickupContentInfo(gpc.pickupIndex, out Color itemCol, isInteractable: false, isUnchosenCommandCube: isUnchosenCommand);
            if (itemCol != Color.clear) overrideColor = itemCol;

            string baseName = ShowNames ? (!string.IsNullOrEmpty(contentName) ? contentName : "Pickup") : "";
            string distStr = ShowDistance ? $"{Mathf.RoundToInt(dist)}m" : "";

            return BuildMultiLine(baseName, "", distStr);
        }

        private static string GetPickupContentInfo(PickupIndex pickupIndex, out Color contentColor, bool isInteractable = false, bool isUnchosenCommandCube = false)
        {
            contentColor = InteractableColor;
            if (pickupIndex == PickupIndex.none) return null;

            try
            {
                PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                if (pickupDef == null) return null;

                contentColor = pickupDef.baseColor != Color.clear ? pickupDef.baseColor : InteractableColor;

                bool isCommandActive = IsCommandArtifactActive();

                // If Artifact of Command is active and this is an interactable (chest/terminal) or an unchosen Command Cube
                if (isCommandActive && (isInteractable || isUnchosenCommandCube || (pickupDef.itemIndex == ItemIndex.None && pickupDef.equipmentIndex == EquipmentIndex.None)))
                {
                    return GetTierRarityName(pickupDef.itemTier);
                }

                // Otherwise, get the specific item or equipment name (for normal items or items already chosen from Command cubes)
                if (pickupDef.itemIndex != ItemIndex.None || pickupDef.equipmentIndex != EquipmentIndex.None)
                {
                    string name = Language.GetString(pickupDef.nameToken);
                    if (string.IsNullOrEmpty(name) || name == pickupDef.nameToken)
                        name = pickupDef.internalName;
                    return name;
                }

                if (pickupDef.itemTier != ItemTier.AssignedAtRuntime)
                {
                    return GetTierRarityName(pickupDef.itemTier);
                }

                string fallbackName = Language.GetString(pickupDef.nameToken);
                if (string.IsNullOrEmpty(fallbackName) || fallbackName == pickupDef.nameToken)
                    fallbackName = pickupDef.internalName;
                return fallbackName;
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
                case ItemTier.Tier1: return "Common";
                case ItemTier.Tier2: return "Uncommon";
                case ItemTier.Tier3: return "Legendary";
                case ItemTier.Boss: return "Boss";
                case ItemTier.Lunar: return "Lunar";
                case ItemTier.VoidTier1:
                case ItemTier.VoidTier2:
                case ItemTier.VoidTier3:
                case ItemTier.VoidBoss: return "Void";
                default: return "Equipment";
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
            Vector2 size = _labelStyle.CalcSize(content);

            float labelWidth = Mathf.Max(size.x + 24f, 400f);
            float labelHeight = size.y + 12f;

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
