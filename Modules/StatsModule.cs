using System;
using HarmonyLib;
using RoR2;

namespace PoppyMenu
{
    internal class StatsModule : PoppyModule
    {
        internal override string Name => "Stats";

        internal static bool DamageOn, AttackSpeedOn, MoveSpeedOn, ArmorOn, CritOn, MaxHealthOn;
        internal static float DamageMult = 1f, AttackSpeedMult = 1f, MoveSpeedMult = 1f, MaxHealthMult = 1f;
        internal static float ArmorBonus = 0f;
        internal static float CritBonus = 100f;

        internal static float ArmorMult { get => ArmorBonus; set => ArmorBonus = value; }
        internal static float CritMult { get => CritBonus; set => CritBonus = value; }

        private static Harmony _harmony;

        private static bool AnyOn =>
            DamageOn || AttackSpeedOn || MoveSpeedOn || ArmorOn || CritOn || MaxHealthOn;

        internal static bool Active => AnyOn;

        internal static void DisableAll()
        {
            DamageOn = AttackSpeedOn = MoveSpeedOn = ArmorOn = CritOn = MaxHealthOn = false;
        }

        private static void EnsurePatched()
        {
            if (_harmony != null)
                return;
            _harmony = new Harmony("poppy.stats");
            var orig = AccessTools.Method(typeof(CharacterBody), nameof(CharacterBody.RecalculateStats));
            var post = AccessTools.Method(typeof(StatsModule), nameof(RecalcPostfix));
            _harmony.Patch(orig, postfix: new HarmonyMethod(post));
        }

        private static void RecalcPostfix(CharacterBody __instance)
        {
            if (__instance == null || __instance != PlayerContext.Body)
                return;
            if (DamageOn) __instance.damage *= DamageMult;
            if (AttackSpeedOn) __instance.attackSpeed *= AttackSpeedMult;
            if (MoveSpeedOn) __instance.moveSpeed *= MoveSpeedMult;
            if (ArmorOn) __instance.armor += ArmorBonus;
            if (CritOn) __instance.crit += CritBonus;
            if (MaxHealthOn) __instance.maxHealth *= MaxHealthMult;
        }

        private static float _prevDmg, _prevAtk, _prevMove, _prevArmor, _prevCrit, _prevHp;
        private static bool _prevDmgOn, _prevAtkOn, _prevMoveOn, _prevArmorOn, _prevCritOn, _prevHpOn;
        private static CharacterBody _prevBody;

        internal override void Tick()
        {
            EnsurePatched();
            if (!PlayerContext.HasBody) return;

            CharacterBody body = PlayerContext.Body;
            bool changed = body != _prevBody ||
                _prevDmgOn != DamageOn || _prevDmg != DamageMult ||
                _prevAtkOn != AttackSpeedOn || _prevAtk != AttackSpeedMult ||
                _prevMoveOn != MoveSpeedOn || _prevMove != MoveSpeedMult ||
                _prevArmorOn != ArmorOn || _prevArmor != ArmorBonus ||
                _prevCritOn != CritOn || _prevCrit != CritBonus ||
                _prevHpOn != MaxHealthOn || _prevHp != MaxHealthMult;

            if (changed)
            {
                _prevBody = body;
                _prevDmgOn = DamageOn; _prevDmg = DamageMult;
                _prevAtkOn = AttackSpeedOn; _prevAtk = AttackSpeedMult;
                _prevMoveOn = MoveSpeedOn; _prevMove = MoveSpeedMult;
                _prevArmorOn = ArmorOn; _prevArmor = ArmorBonus;
                _prevCritOn = CritOn; _prevCrit = CritBonus;
                _prevHpOn = MaxHealthOn; _prevHp = MaxHealthMult;

                body.RecalculateStats();
            }
        }

        internal override void DrawMenu()
        {
            Widgets.SectionBegin("Stat Multipliers & Bonuses");

            DamageOn = Widgets.Toggle("Damage Multiplier", DamageOn);
            DamageMult = Widgets.Slider("Damage x", DamageMult, 1f, 100f);

            AttackSpeedOn = Widgets.Toggle("Attack Speed Multiplier", AttackSpeedOn);
            AttackSpeedMult = Widgets.Slider("Atk Spd x", AttackSpeedMult, 1f, 100f);

            MoveSpeedOn = Widgets.Toggle("Move Speed Multiplier", MoveSpeedOn);
            MoveSpeedMult = Widgets.Slider("Move x", MoveSpeedMult, 1f, 100f);

            ArmorOn = Widgets.Toggle("Armor Bonus", ArmorOn);
            ArmorBonus = Widgets.Slider("Armor +", ArmorBonus, 0f, 1000f);

            CritOn = Widgets.Toggle("Crit Chance Bonus", CritOn);
            CritBonus = Widgets.Slider("Crit +%", CritBonus, 0f, 1000f);

            MaxHealthOn = Widgets.Toggle("Max Health Multiplier", MaxHealthOn);
            MaxHealthMult = Widgets.Slider("Health x", MaxHealthMult, 1f, 100f);
            Widgets.SectionEnd();

            Widgets.SectionBegin("Current Stats");
            if (PlayerContext.HasBody)
            {
                CharacterBody b = PlayerContext.Body;
                Widgets.Label($"Max Health: {b.maxHealth:0}");
                Widgets.Label($"Damage: {b.damage:0.##}");
                Widgets.Label($"Attack Speed: {b.attackSpeed:0.##}");
                Widgets.Label($"Armor: {b.armor:0.##}");
                Widgets.Label($"Crit Chance: {b.crit:0.##}%");
                Widgets.Label($"Move Speed: {b.moveSpeed:0.##}");
            }
            else
            {
                Widgets.Label("(no body)");
            }
            Widgets.SectionEnd();
        }

        internal override void OnUnload()
        {
            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
                _harmony = null;
            }
        }
    }
}
