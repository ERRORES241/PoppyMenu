# Rule: Decompiled Code Inspection & Precise Asset Referencing

1. **Always Inspect Decompiled Code (`src/`)**:
   - Before modifying or adding any feature in Risk of Rain 2, search and inspect the decompiled source files under `src/`.
   - Never guess asset names, component field names, or state class names out of nowhere.

2. **Direct Field & Object Dereferencing**:
   - Prefer accessing exact component fields (e.g., `ShrineRebirthController.solusShopPortalISC`, `TeleporterInteraction.shopPortalSpawnCard`) rather than relying on loose string guessing.
   - If an asset is missing or not a standard `SpawnCard`, inspect its actual container component in `src/`.

3. **Strict Scope Control & No Unsolicited Scanners**:
   - Implement only what the user requests. Do not add arbitrary dynamic scanners or extra UI sections unless explicitly asked.
   - When the user asks to remove a module or section, remove it completely without leaving dead code.
