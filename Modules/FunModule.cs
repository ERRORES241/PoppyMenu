using RoR2;
using UnityEngine;

namespace PoppyMenu
{
    internal class FunModule : PoppyModule
    {
        internal override string Name => "Fun";

        internal static bool RailgunnerInstaCharge;
        internal static bool RailgunnerNoUltCooldown;
        internal static bool RailgunnerUltSpam;

        internal override void Tick()
        {
            if (!PlayerContext.InGame || !PlayerContext.HasBody) return;

            CharacterBody body = PlayerContext.Body;
            if (body == null) return;

            if (RailgunnerUltSpam)
            {
                RailgunnerInstaCharge = true;
                RailgunnerNoUltCooldown = true;
            }

            if (RailgunnerInstaCharge || RailgunnerNoUltCooldown || RailgunnerUltSpam)
            {
                // 1. Reset Special skill cooldown ONLY (Do NOT touch primary, secondary, utility)
                if (body.skillLocator != null && body.skillLocator.special != null)
                {
                    body.skillLocator.special.stock = body.skillLocator.special.maxStock;
                    body.skillLocator.special.rechargeStopwatch = 999f;
                }

                EntityStateMachine[] machines = body.GetComponents<EntityStateMachine>();
                if (machines != null)
                {
                    foreach (var machine in machines)
                    {
                        if (machine == null || machine.state == null) continue;

                        // Instant Charge logic: set fixedAge to 999f on both weapon and backpack so the UI bar renders 100% full instantly and transitions naturally
                        if (RailgunnerInstaCharge)
                        {
                            if (machine.state is EntityStates.Railgunner.Weapon.BaseChargeSnipe chargeWeapon)
                            {
                                chargeWeapon.fixedAge = 999f;
                            }
                            if (machine.state is EntityStates.Railgunner.Backpack.BaseCharging chargeBackpack)
                            {
                                chargeBackpack.fixedAge = 999f;
                            }
                        }

                        // Remove Overheat/Expired lockout after Special fires
                        if (RailgunnerNoUltCooldown || RailgunnerUltSpam)
                        {
                            string stateName = machine.state.GetType().Name;
                            if (stateName.Contains("Expired") || stateName.Contains("Offline") || stateName.Contains("Reboot") || stateName.Contains("Overheat"))
                            {
                                machine.SetNextStateToMain();
                            }
                        }

                        // Ult Spam on M1 (Primary / LMB): every M1 press fires a Supercharge Ult shot
                        if (RailgunnerUltSpam && body.inputBank != null && body.inputBank.skill1.down)
                        {
                            if (machine.customName == "Weapon" || machine.state is EntityStates.Railgunner.Weapon.FirePistol || machine.state is EntityStates.Railgunner.Weapon.FireSnipeLight || machine.state is EntityStates.Railgunner.Weapon.FireSnipeHeavy)
                            {
                                if (!(machine.state is EntityStates.Railgunner.Weapon.FireSnipeSuper))
                                {
                                    machine.SetNextState(new EntityStates.Railgunner.Weapon.FireSnipeSuper());
                                }
                            }
                        }
                    }
                }
            }
        }

        internal override void DrawMenu()
        {
            Widgets.SectionBegin("Railgunner");
            RailgunnerInstaCharge = Widgets.Toggle("Instant Ult Charge", RailgunnerInstaCharge);
            Widgets.Hint("Special skill charges instantly (0s charge time).");

            RailgunnerNoUltCooldown = Widgets.Toggle("No Ult Cooldown (Special Only)", RailgunnerNoUltCooldown);
            Widgets.Hint("Resets Special skill stock and clears 5s overheat lockout without touching other skills.");

            RailgunnerUltSpam = Widgets.Toggle("Ult Spam (Every Shot = Ult)", RailgunnerUltSpam);
            Widgets.Hint("Converts EVERY sniper shot into a Supercharge Ultimate bullet for non-stop ult spam.");
            Widgets.SectionEnd();
        }
    }
}
