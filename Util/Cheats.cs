using RoR2;
using UnityEngine;

namespace PoppyMenu
{
    internal static class Cheats
    {
        internal static void DisableAll()
        {
            PlayerModule.GodMode = PlayerModule.InfiniteSkills = false;
            Aim.Enabled = false; Aim.MagicBullet = false; Aim.Target = null;
            MovementModule.Flight = MovementModule.NoClip = MovementModule.AlwaysSprint = MovementModule.JumpPack = false;
            ItemsModule.NoEquipmentCooldown = false;
            RenderModule.EspMobs = RenderModule.EspInteractables = RenderModule.EspTeleporter = false;
            StatsModule.DisableAll();
            WorldModule.FreezeMatch = false; WorldModule.FreezeTimer = false; WorldModule.GuaranteedShrineChance = false; WorldModule.TimeScale = 1f;
            Notify.Push("All features disabled");
        }
    }
}
