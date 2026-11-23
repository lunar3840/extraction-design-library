# Post-apply production source and pure-model review

- Performed against actual applied production files; this delegate made no Assets, Library, Editor, request-file, scene, profile or runtime-state changes.
- This is a new follow-up under `post_apply_01`; original offline logs and original source-hashes.json remain untouched.
- Current Assembly-CSharp.dll: `314C52B33F96AF53093E2292A3D4E13A799FC5D5C0200E0AA2C8F97E65F5B74F`.
- Fresh standalone Roslyn compile exit: 0. Fresh process runner exit: 0. Three observed input hashes stable before/after: true.
- Double-click sequence: 24 assertions. Rig/grid/loot: 17. Finite transfer/region exchange: 10. Automatic reload: 38. Total: 89 passed.
- The finite-transfer suite additionally checks item-count conservation on every one of 10,000 seeded operations.

## Actual source vs saved baseline and reviewed v3

All 14 backed-up Runtime files differ from production_before as expected. All 14 actual files exactly match the reviewed v3 merged candidates after CRLF/LF normalization. Eight also match byte-for-byte; six only differ in line endings. Actual new FigmaGameSkin.cs also has no textual diff against staging/FigmaGameSkin.cs.

The reviewed baseline differences are shared font/surface/key/button substitutions and presentation spacing, plus the explicitly reviewed title preview exception:
- Existing user title/story, save-slot selection/confirmation/cancel callbacks and transition flow are retained.
- Panel and Tile remain paint-only. Controls still use the existing button/Press routes; no inventory or combat transaction was added.
- Title has an isolated portrait render exception while the frontend still applies BlocksGameplay, closes InventoryOpen, sets bagUI.ShowUI false, and calls world.Freeze.
- InventoryPortrait framing still calculates inventoryPortrait first, selects 1.16 only for lobby/title without inventory, otherwise 1.06, with FrameEquipment=false; latest reaction/rotation methods remain.
- Medical status/recommendation and treatment progress overlap changes affect drawing rectangles only.
- Latest lobby footer ownership and current values were preserved. No y896-to-y912 edit was performed by this delegate.

No new source-delta regression was found relative to the staged integration reviewed earlier.

## Strict scope of this follow-up

These are in-memory domain/model assertions, not actual UI/native input tests. SequenceProgram and AutomaticReloadRules are compiled directly from current production rule sources; InventoryState, LootState and RifleState come from the current actual compiled production assembly. The runner does not instantiate Unity GameObjects or scenes, open Unity, trigger UI routes, touch user save data or write production. It cannot validate rendering, mouse/native input, screenshot layout, menu UI controller integration or all interaction sequences. Those checks remain with the main agent's real before/after route verification.

Only new workspace artifacts, standalone executable/configuration and the runner's workspace-relative SequenceChecks.results.txt are written.

## Evidence

- `RegressionMain.rsp`, `RegressionMain.runtimeconfig.json`, `RegressionMain.dll`: standalone follow-up compile/run inputs.
- `compile.txt`, `results.txt`, `SequenceChecks.results.txt`: fresh observed outputs.
- `source-hashes.before.json`, `source-hashes.after.json`: actual compiled DLL plus direct current rules, observed before/after.
- `runtime-source-comparison.json`: per-file baseline, actual and staged SHA256 plus normalized text comparison.

