using RoR2;
using UnityEngine;

namespace PoppyMenu
{
    internal class TeleporterModule : PoppyModule
    {
        internal override string Name => "Teleport";

        internal static bool InstaCharge;

        internal override void Tick()
        {
            if (InstaCharge && TeleporterInteraction.instance != null && TeleporterInteraction.instance.chargeFraction < 0.99f)
            {
                NetUtil.Do(PoppyOp.ChargeTeleporter);
            }
        }

        internal override void DrawMenu()
        {
            if (!PlayerContext.InGame) { Widgets.Label("Start a run first."); return; }

            Widgets.SectionBegin("Teleporter");
            TeleporterInteraction tp = TeleporterInteraction.instance;
            Widgets.Label(tp != null ? "Charge: " + Mathf.FloorToInt(tp.chargeFraction * 100f) + "%" : "No teleporter on this stage.");
            InstaCharge = Widgets.Toggle("Instant Charge (hold)", InstaCharge);
            Widgets.Button("Skip Stage", () => NetUtil.Do(PoppyOp.SkipStage));
            Widgets.Button("Add Mountain Shrine Stack", () => NetUtil.Do(PoppyOp.AddMountainShrine));
            Widgets.SectionEnd();
        }
    }
}
