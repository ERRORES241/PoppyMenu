using HarmonyLib;
using RoR2;
using UnityEngine;

namespace PoppyMenu
{
    internal class WorldModule : PoppyModule
    {
        internal override string Name => "Time";

        internal static bool FreezeMatch;
        internal static bool FreezeTimer;
        internal static float TimeScale = 1f;
        internal static bool GuaranteedShrineChance;

        internal static bool InfiniteShrineChance;

        private static bool _controllingTime;
        private static bool _timerFrozen;
        private static Harmony _harmony;

        private static readonly AccessTools.FieldRef<ShrineChanceBehavior, int> _successfulPurchaseCountRef =
            AccessTools.FieldRefAccess<ShrineChanceBehavior, int>("successfulPurchaseCount");
        private static readonly AccessTools.FieldRef<ShrineChanceBehavior, bool> _waitingForRefreshRef =
            AccessTools.FieldRefAccess<ShrineChanceBehavior, bool>("waitingForRefresh");
        private static readonly AccessTools.FieldRef<ShrineChanceBehavior, float> _refreshTimerRef =
            AccessTools.FieldRefAccess<ShrineChanceBehavior, float>("refreshTimer");
        private static readonly AccessTools.FieldRef<ShrineChanceBehavior, bool> _chanceDollWinRef =
            AccessTools.FieldRefAccess<ShrineChanceBehavior, bool>("chanceDollWin");
        private static readonly AccessTools.FieldRef<ShrineChanceBehavior, Xoroshiro128Plus> _rngRef =
            AccessTools.FieldRefAccess<ShrineChanceBehavior, Xoroshiro128Plus>("rng");

        internal static void Init()
        {
            try
            {
                _harmony = new Harmony("poppy.world");
                var addStack = AccessTools.Method(typeof(ShrineChanceBehavior), nameof(ShrineChanceBehavior.AddShrineStack));
                if (addStack != null)
                {
                    _harmony.Patch(addStack, prefix: new HarmonyMethod(typeof(WorldModule), nameof(AddShrineStack_Prefix)));
                }
            }
            catch (System.Exception e)
            {
                Log.Error("WorldModule Harmony patch failed: " + e);
            }
        }

        private static bool AddShrineStack_Prefix(ShrineChanceBehavior __instance, Interactor activator)
        {
            if (!GuaranteedShrineChance || __instance == null || !UnityEngine.Networking.NetworkServer.active)
                return true;

            CharacterBody body = activator != null ? activator.GetComponent<CharacterBody>() : null;
            if (body == null || body.inventory == null)
                return true;

            Xoroshiro128Plus rng = _rngRef(__instance);
            if (rng == null)
            {
                ulong seed = Run.instance != null ? Run.instance.treasureRng.nextUlong : (ulong)Random.Range(1, 100000);
                rng = new Xoroshiro128Plus(seed);
                _rngRef(__instance) = rng;
            }

            UniquePickup pickup = UniquePickup.none;
            bool chanceDollWin = false;

            // 1. Try dropTable
            if (__instance.dropTable != null)
            {
                try { pickup = __instance.dropTable.GeneratePickup(rng); } catch { }
            }

            // 2. Fallback to random available items if dropTable fails or returns invalid
            if (!pickup.isValid)
            {
                System.Collections.Generic.List<PickupIndex> candidates = new System.Collections.Generic.List<PickupIndex>();
                if (Run.instance != null)
                {
                    if (Run.instance.availableTier2DropList != null && Run.instance.availableTier2DropList.Count > 0)
                        candidates.AddRange(Run.instance.availableTier2DropList);
                    if (Run.instance.availableTier1DropList != null && Run.instance.availableTier1DropList.Count > 0)
                        candidates.AddRange(Run.instance.availableTier1DropList);
                    if (Run.instance.availableTier3DropList != null && Run.instance.availableTier3DropList.Count > 0)
                        candidates.AddRange(Run.instance.availableTier3DropList);
                }
                if (candidates.Count > 0)
                {
                    PickupIndex chosen = rng.NextElementUniform<PickupIndex>(candidates);
                    pickup = new UniquePickup(chosen);
                }
            }

            // 3. Last fallback: any pickup in PickupCatalog
            if (!pickup.isValid)
            {
                if (PickupCatalog.pickupCount > 0)
                {
                    PickupIndex fallback = new PickupIndex(rng.RangeInt(0, PickupCatalog.pickupCount));
                    pickup = new UniquePickup(fallback);
                }
            }

            _chanceDollWinRef(__instance) = chanceDollWin;

            // 4. Update success count
            int currentSuccess = _successfulPurchaseCountRef(__instance) + 1;
            if (InfiniteShrineChance) currentSuccess = 0;
            else _successfulPurchaseCountRef(__instance) = currentSuccess;

            // 5. Create pickup droplet
            if (__instance.dropletOrigin != null && pickup.isValid)
            {
                PickupDropletController.CreatePickupDroplet(pickup, __instance.dropletOrigin.position, __instance.dropletOrigin.forward * 20f);
            }

            // 6. Send broadcast chat
            Chat.SubjectFormatChatMessage message = new Chat.SubjectFormatChatMessage
            {
                subjectAsCharacterBody = body,
                baseToken = "SHRINE_CHANCE_SUCCESS_MESSAGE"
            };
            Chat.SendBroadcastChat(message);

            // 7. Fire global event
            try
            {
                var evtRef = AccessTools.StaticFieldRefAccess<System.Action<bool, Interactor>>(typeof(ShrineChanceBehavior), "onShrineChancePurchaseGlobal");
                if (evtRef != null) evtRef(false, activator);
            }
            catch { }

            // 8. Trigger refresh cooldown
            _waitingForRefreshRef(__instance) = true;
            _refreshTimerRef(__instance) = 2f;

            // 9. Spawn reward VFX
            if (__instance.effectPrefabShrineRewardNormal != null)
            {
                EffectManager.SpawnEffect(__instance.effectPrefabShrineRewardNormal, new EffectData
                {
                    origin = __instance.transform.position,
                    rotation = Quaternion.identity,
                    scale = 1f,
                    color = (Color32)__instance.colorShrineRewardNormal
                }, true);
            }

            // 10. Check max purchase limit
            if (!InfiniteShrineChance && currentSuccess >= __instance.maxPurchaseCount)
            {
                if (__instance.symbolTransform != null && __instance.symbolTransform.gameObject != null)
                    __instance.symbolTransform.gameObject.SetActive(false);
                try { __instance.CallRpcSetPingable(false); } catch { }
            }

            return false;
        }

        internal override void Tick()
        {
            if (NetUtil.IsServer && Run.instance != null)
            {
                if (FreezeTimer) Run.instance.SetRunStopwatchPaused(true);
                else if (_timerFrozen) Run.instance.SetRunStopwatchPaused(false);
            }
            _timerFrozen = FreezeTimer;

            ApplyTimeScale();
        }

        private static void ApplyTimeScale()
        {
            bool want = FreezeMatch || Mathf.Abs(TimeScale - 1f) > 0.001f;
            if (want)
            {
                Time.timeScale = FreezeMatch ? 0f : TimeScale;
                _controllingTime = true;
            }
            else if (_controllingTime)
            {
                if (!PauseManager.isPaused) Time.timeScale = 1f;
                _controllingTime = false;
            }
        }

        internal static void RestoreTime()
        {
            FreezeMatch = false;
            if (_controllingTime)
            {
                if (!PauseManager.isPaused) Time.timeScale = 1f;
                _controllingTime = false;
            }
            if (_timerFrozen)
            {
                if (NetUtil.IsServer && Run.instance != null) Run.instance.SetRunStopwatchPaused(false);
                _timerFrozen = false;
            }
            FreezeTimer = false;
        }

        internal override void OnUnload() => RestoreTime();

        internal override void DrawMenu()
        {
            Widgets.SectionBegin("Time");
            FreezeMatch = Widgets.Toggle("Freeze Match (everything stops)", FreezeMatch);
            Widgets.Hint("Stops everything, including you. Toggle off to move. Host or solo.");

            FreezeTimer = Widgets.Toggle("Freeze Timer (difficulty)", FreezeTimer);
            Widgets.Hint("Stops the run clock so difficulty quits climbing. Enemies still move.");

            GUILayout.Space(4);
            TimeScale = Widgets.Slider("Time Scale", TimeScale, 0.1f, 3f);
            GUILayout.BeginHorizontal();
            Widgets.Button("Slow-mo (0.25x)", () => TimeScale = 0.25f);
            Widgets.Button("Normal (1x)", () => TimeScale = 1f);
            Widgets.Button("Fast (2x)", () => TimeScale = 2f);
            GUILayout.EndHorizontal();
            Widgets.SectionEnd();

            Widgets.SectionBegin("Shrines");
            GuaranteedShrineChance = Widgets.Toggle("100% Shrine of Chance Win Rate", GuaranteedShrineChance);
            Widgets.Hint("Guarantees an item drop on every offer at Shrine of Chance (no fails). Host or solo.");
            InfiniteShrineChance = Widgets.Toggle("Infinite Shrine of Chance Uses", InfiniteShrineChance);
            Widgets.Hint("Allows using Shrine of Chance infinitely without it shutting down.");
            Widgets.SectionEnd();

            Widgets.SectionBegin("Safety");
            Safety.NoEnemies = Widgets.Toggle("No Enemies (kill on spawn)", Safety.NoEnemies);
            Safety.LockExp = Widgets.Toggle("Lock Experience", Safety.LockExp);
            Safety.PreventProfileWriting = Widgets.Toggle("Prevent Profile Saving", Safety.PreventProfileWriting);
            Widgets.Hint("Host or solo. Prevent Profile Saving keeps test runs out of your save file.");
            Widgets.SectionEnd();
        }
    }
}
