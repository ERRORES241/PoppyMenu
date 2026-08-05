# Rule: Risk of Rain 2 State Machine & UI Overlay Synchronization

1. **Preserve UI Overlay Animations**:
   - When accelerating state machine durations (e.g., instant charges like Railgunner's `BaseChargeSnipe`), NEVER call `SetNextStateToMain()` immediately inside `Tick()` / `Update()`.
   - Calling `SetNextStateToMain()` prematurely aborts the state machine before `BaseCharging` / `ImageFillController` can render its UI overlay.

2. **Synchronize Weapon and Backpack State Machines**:
   - Set `fixedAge = 999f` (or `>= duration`) on **both** the weapon state machine (`BaseChargeSnipe`) and the backpack/UI state machine (`BaseCharging`).
   - This allows `FixedUpdate()` on the backpack state machine to set `fillUi.SetT(1.0)` (rendering a 100% full charge bar) on the next frame, followed by a natural transition into ready mode.
