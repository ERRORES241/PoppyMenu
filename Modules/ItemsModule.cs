using System.Collections.Generic;
using RoR2;

namespace PoppyMenu
{
    internal class ItemsModule : PoppyModule
    {
        internal override string Name => "Items";

        internal static bool NoEquipmentCooldown;
        internal static int GiveCount = 1;
        internal static bool SaleStarCheat;
        internal static int SaleStarDropCount = 5;

        internal override void DrawMenu()
        {
            Widgets.SectionBegin("Give");
            GiveCount = Widgets.IntStepper("Count", GiveCount, 1, 1, 1000);

            Widgets.Button("Give Item...", () =>
                ItemPicker.Open("Items", idx => NetUtil.Do(PoppyOp.GiveItem, (int)idx, GiveCount)));

            var equipRows = new List<ListPicker.Row>(Catalogs.Equipment.Count);
            foreach (var entry in Catalogs.Equipment)
            {
                var e = entry;
                equipRows.Add(new ListPicker.Row(
                    e.Name, e.Color,
                    () => NetUtil.Do(PoppyOp.GiveEquipment, (int)e.Index)));
            }
            Widgets.PickerButton("Give Equipment...", "Equipment", equipRows);
            Widgets.Button($"Give All Items (x{GiveCount})", () => NetUtil.Do(PoppyOp.GiveAllItems, i2: GiveCount));
            Widgets.SectionEnd();

            Widgets.SectionBegin("Inventory");
            Widgets.Button("Stack (Shrine of Order)", () => NetUtil.Do(PoppyOp.StackInventory));
            Widgets.Button("Reroll Items", () => NetUtil.Do(PoppyOp.RollItems));
            Widgets.Button("Undo Last Item Change", () => NetUtil.Do(PoppyOp.UndoInventory));
            NoEquipmentCooldown = Widgets.Toggle("No Equipment Cooldown", NoEquipmentCooldown);
            Widgets.ConfirmButton("items.clearinv", "Clear Inventory", () => NetUtil.Do(PoppyOp.ClearInventory));
            Widgets.SectionEnd();

            Widgets.SectionBegin("Sale Star (Выгодная звезда)");
            SaleStarCheat = Widgets.Toggle("Force Drop Count", SaleStarCheat);
            if (SaleStarCheat)
            {
                SaleStarDropCount = Widgets.IntStepper("Drop Count", SaleStarDropCount, 1, 2, 5);
            }
            Widgets.SectionEnd();
        }

        internal static void Init()
        {
            On.RoR2.PurchaseInteraction.OnInteractionBegin += PurchaseInteraction_OnInteractionBegin;
        }

        private static void PurchaseInteraction_OnInteractionBegin(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            var body = activator.GetComponent<CharacterBody>();
            int prevConsumed = 0;
            if (body && body.inventory)
            {
                prevConsumed = body.inventory.GetItemCount(DLC2Content.Items.LowerPricedChestsConsumed);
            }

            orig(self, activator);

            if (SaleStarCheat && body && body.inventory)
            {
                int postConsumed = body.inventory.GetItemCount(DLC2Content.Items.LowerPricedChestsConsumed);
                if (postConsumed > prevConsumed)
                {
                    var chest = self.GetComponent<ChestBehavior>();
                    if (chest) chest.dropCount = SaleStarDropCount;

                    var roulette = self.GetComponent<RouletteChestController>();
                    if (roulette) roulette.dropCount = SaleStarDropCount;
                }
            }
        }

        internal override void Tick()
        {
            if (!NoEquipmentCooldown || !NetUtil.IsServer || !PlayerContext.HasBody)
                return;

            EquipmentSlot slot = PlayerContext.Body.equipmentSlot;
            if (slot == null || slot.equipmentIndex == EquipmentIndex.None)
                return;

            int max = slot.maxStock > 0 ? slot.maxStock : 1;
            if (slot.stock < max)
                slot.stock = max;
        }
    }
}
