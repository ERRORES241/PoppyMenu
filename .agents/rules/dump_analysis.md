# Rule: Dump Analysis & Verification

Before introducing new functionality, modifying existing methods, or referencing Risk of Rain 2 types, methods, fields, or assets:
1. **Always inspect the decompiled source in `src/`**: Verify exact class names, field names, property signatures, enum values, and state machine structures.
2. **Never guess or assume APIs**: Do not invent class names, method signatures, or asset strings out of nowhere without checking `src/` first.
3. **Verify exact component interactions**: When interacting with RoR2 components (e.g. `CharacterBody`, `EntityStateMachine`, `TeleporterInteraction`, `PurchaseInteraction`, `SpawnCard`), inspect the decompiled implementation in `src/RoR2` to ensure contracts and network RPCs/Cmds match exactly.
