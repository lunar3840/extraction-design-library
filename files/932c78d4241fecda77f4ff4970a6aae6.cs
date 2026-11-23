#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Escape.Enemy;
using Escape.Inventory;
using Escape.Player;
using Escape.Stage.Blockout;
using Escape.SurfaceIvy;
using Escape.Weapon;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Escape.EditorTools.ClinicMoodTrial
{
    // Staging source. The sole Unity owner copies it into Editor, invokes one public step,
    // reads the result, and removes the temporary copy after the trial. No auto-run.
    [InitializeOnLoad]
    public static class StageAClinicMoodTrialValidation
    {
        const string Key = "Escape.ClinicMoodTrial.Validation.20261030";
        const string Record = "C:/Users/ehoji/OneDrive/문서/ChatGPT/익스트랙션/outputs/change_records/2026-10-30/a-map-clinic-reclaimed-mood-trial";
        const string PostUiSubfolder = "rounds/post_ui_checkpoint";
        static bool IsPostUiCheckpoint => SessionState.GetBool(Key + ".PostUiCheckpoint", false);
        static string CurrentRecord => IsPostUiCheckpoint
            ? Path.Combine(Record, PostUiSubfolder) : Record;
        // Builder may change this one name before installation; it must be a new root.
        const string AddedRootName = "__A34_ClinicMoodTrial";
        const string AddedAssetPrefix = "Assets/Escape/Art/ClinicMoodTrial20261030/";
        const int ExpectedReferencedAssetCount = 8497;
        const int MaxAddedTriangles = 5000, MaxAddedVertices = 7000;
        const int Width = 1920, Height = 1080, Warmup = 45, SettleFrames = 20,
            ResolutionTimeoutFrames = 180, SampleFrames = 90;
        const int Seed = 10302026;
        const string ExpectedHudFingerprint =
            "weapon=AR_001 ammo=item_018 magazine=30 reserve=70 weightKg=19.8 reloading=False";
        const string BaselineFile = "staging/validation_tool/BEFORE_INVENTORY.tsv";
        const string ProtectedBeforeFile = "staging/validation_tool/BEFORE_PROTECTED_FILES.tsv";
        const string AssetsBeforeFile = "staging/validation_tool/BEFORE_REFERENCED_ASSETS.tsv";
        const string RootBeforeFile = "staging/validation_tool/RECONSTRUCTED_ROOT_BEFORE.tsv";
        const string RootAssetsBeforeFile = "staging/validation_tool/RECONSTRUCTED_ROOT_ASSETS_BEFORE.tsv";
        const string ReconstructionSourceFile = "staging/validation_tool/RECONSTRUCTION_SOURCE.txt";
        static readonly string[] ProtectedFiles = {
            "Assets/Escape/Runtime/Stage/Blockout/StageABuildingV33Reveal.cs",
            "Assets/Escape/Art/BuildingV33/Reveal/BuildingV33LocalReveal.shader",
            "Assets/Escape/Runtime/Combat/CombatFeedbackHud.cs",
            "Assets/Escape/Runtime/Combat/CombatFeedbackAudio.cs"
        };

        [Serializable] sealed class View
        {
            public string id, status, error, gameImage, cameraImage, player, eye, target,
                navSampleSource, requestedFeet,
                rootRendererStates, revealBlockingSurface, loadoutFingerprint, operationTimeText,
                sourceShaderNames, growthRendererStates, groundRendererStates;
            public bool gameExists, cameraExists;
            public bool revealActive;
            public int samples, rootRenderers, rootTriangles, rootActiveRenderers, rootShadowCasters,
                revealActiveRenderers, growthActiveRenderers, groundActiveRenderers;
            public float fov;
            public float editorMedianMs, editorP95Ms, frameCpuMedianMs, frameGpuMedianMs,
                drawCallsMedian, sceneTrianglesMedian;
        }
        [Serializable] sealed class Report
        {
            public string phase, status, error, startedUtc, finishedUtc, scene, store, opening,
                rootName, rendererScope, timingScope, movementStatus, movementStart, movementEnd,
                environmentFingerprint, shaderTimeLimit, comparisonContext, reconstructionBasis,
                hiddenRootPath, rootAssetSnapshot, loadoutBeforeFixture, loadoutFingerprint,
                loadoutFixtureMethod, operationTimerAtFreeze, revealSupplementStatus;
            public int width, height, seed, addedColliders, addedRenderers, addedTriangles,
                walkSteps, viewCount;
            public bool temporaryStore, liveStageA, currentSceneClean, sceneWasSaved,
                randomStateRestored, originalStateRestored, reconstructedBefore,
                rootHiddenDuringCapture, rootWasActive, rootStateRestored;
            public View[] views;
        }
        [Serializable] sealed class AcceptedBeforeFlag
        {
            public string reportSha256 = null;
            public bool manualVisualReview = false;
        }
        sealed class Pose
        {
            public string id;
            public Vector3 feet, eye, target;
            public bool gameplayOrbit;
        }

        static readonly List<View> views = new List<View>();
        static readonly List<float> editorMs = new List<float>(), cpuMs = new List<float>(),
            gpuMs = new List<float>(), draws = new List<float>(), triangles = new List<float>();
        static readonly FrameTiming[] timing = new FrameTiming[1];
        static readonly Dictionary<EnemyController, (bool frozen, bool ticks)> enemies =
            new Dictionary<EnemyController, (bool, bool)>();
        static StageScenePreview preview;
        static StageABlockoutMap map;
        static PlayerMotor player;
        static PlayerMotor earlyInputPlayer;
        static bool earlyInputWasLive;
        static InventoryController inventory;
        static RifleController rifle;
        static CharacterController body;
        static Camera camera;
        static TopDownCamera follow;
        static StageABuildingD04Validation.Passage opening;
        static Transform reconstructionRoot;
        static bool reconstructionRootWasActive;
        static Pose[] poses;
        static Report report;
        static string phase, output;
        static int index, stage, frames, lastFrame = -1, walkTarget, walkSteps;
        static bool initialized, savedState, oldWorldTicks, oldInput, oldFollow, oldBackground,
            randomCaptured;
        static float oldFov, oldTimeScale;
        static Vector3 oldPlayer, oldCamera;
        static Quaternion oldPlayerRotation, oldCameraRotation;
        static UnityEngine.Random.State oldRandom;
        static Vector3[] walkCorners;
        static View current;

        static StageAClinicMoodTrialValidation() { EditorApplication.update += Poll; }

        public static string SelectPostUiCheckpoint()
        {
            RequireStableEdit();
            SessionState.SetBool(Key + ".PostUiCheckpoint", true);
            return "Selected only " + CurrentRecord + "; original round files remain untouched.";
        }

        public static string SelectOriginalRound()
        {
            RequireStableEdit();
            SessionState.SetBool(Key + ".PostUiCheckpoint", false);
            return "Selected original trial record: " + CurrentRecord;
        }

        public static string SnapshotPostUiCheckpoint()
        {
            RequirePostUiCheckpoint(); RequireCleanEdit();
            var mapEdit = UnityEngine.Object.FindFirstObjectByType<StageABlockoutMap>();
            if (!mapEdit || mapEdit.gameObject.scene.path != StageV08Build.Scene)
                throw new InvalidOperationException("Retained A map is not open.");
            Transform root = FindAddedRoot(mapEdit.transform);
            if (!root || !root.gameObject.activeSelf || root.GetComponentsInChildren<Renderer>(true).Length != 6 ||
                root.GetComponentsInChildren<Collider>(true).Length != 0)
                throw new InvalidOperationException("Saved candidate01 root is not intact and active.");
            string[] inventory = Inventory(mapEdit.gameObject.scene, mapEdit.transform);
            string originalInventory = Path.Combine(Record, BaselineFile);
            if (!File.Exists(originalInventory))
                throw new FileNotFoundException("Original scene inventory missing; cannot reconstruct before.", originalInventory);
            string[] originalRows = File.ReadAllLines(originalInventory);
            if (!EquivalentOriginalSceneAcrossUi(originalRows, inventory, out int uiSerializedRows))
            {
                string diagnostics = WriteInventoryMismatchDiagnostics(originalRows, inventory);
                throw new InvalidOperationException("Original scene hierarchy/geometry/bindings changed beyond V33 UI serialized fields; cannot reconstruct before. Exact diff: " + diagnostics);
            }
            string originalAssets = Path.Combine(Record, AssetsBeforeFile);
            if (!File.Exists(originalAssets)) throw new FileNotFoundException("Original referenced asset baseline missing.");
            string[] sourceAssets = File.ReadAllLines(originalAssets);
            string[] nowAssets = RehashFrozenAssets(sourceAssets, out string assetIssue);
            if (assetIssue != null) throw new InvalidOperationException("Original asset baseline invalid: " + assetIssue);
            for (int i = 0; i < sourceAssets.Length; i++)
            {
                string path = sourceAssets[i].Substring(0, sourceAssets[i].IndexOf('\t'));
                if (IsOriginalMeshOrMaterial(path) && sourceAssets[i] != nowAssets[i])
                    throw new InvalidOperationException("Original mesh/material changed: " + path);
            }
            string[] rootRows = RootInventory(root);
            string[] rootAssetRows = RootAssetHashes(root, out int dedicatedAssetCount);
            if (dedicatedAssetCount != 14)
                throw new InvalidOperationException("Expected candidate01 root's 14 dedicated assets; actual=" + dedicatedAssetCount);
            if (rootAssetRows.Any(row => row.EndsWith("\tMISSING", StringComparison.Ordinal)))
                throw new InvalidOperationException("Candidate01 root asset or dependency is missing; reconstruction denied.");
            string inventoryPath = Path.Combine(CurrentRecord, BaselineFile);
            string protectedPath = Path.Combine(CurrentRecord, ProtectedBeforeFile);
            string assetsPath = Path.Combine(CurrentRecord, AssetsBeforeFile);
            string rootPath = Path.Combine(CurrentRecord, RootBeforeFile);
            string rootAssetsPath = Path.Combine(CurrentRecord, RootAssetsBeforeFile);
            string sourcePath = Path.Combine(CurrentRecord, ReconstructionSourceFile);
            foreach (string path in new[] { inventoryPath, protectedPath, assetsPath, rootPath, rootAssetsPath, sourcePath }) RefuseExisting(path);
            Directory.CreateDirectory(Path.GetDirectoryName(inventoryPath));
            File.WriteAllLines(inventoryPath, inventory, Encoding.UTF8);
            File.WriteAllLines(protectedPath, ProtectedHashes(), Encoding.UTF8);
            File.WriteAllLines(assetsPath, nowAssets, Encoding.UTF8);
            File.WriteAllLines(rootPath, rootRows, Encoding.UTF8);
            File.WriteAllLines(rootAssetsPath, rootAssetRows, Encoding.UTF8);
            File.WriteAllText(sourcePath,
                "POST_UI_RECONSTRUCTION_SOURCE\ncreatedUtc=" + DateTime.UtcNow.ToString("O") +
                "\ncontext=" + PostUiSubfolder +
                "\nsource=Saved candidate01 retained A scene with owned CLINIC trial root active\n" +
                "allowedUiSerializedRows=" + uiSerializedRows + "\n" +
                "hiddenOnlyInPlay=" + PathOf(root) +
                "\nownedRenderers=6\ndedicatedRootAssets=14\n" +
                "originalSceneInventory=" + BaselineFile +
                "\ncurrentPostUiProtectedSources=" + ProtectedBeforeFile +
                "\noriginalReferencedAssets=" + AssetsBeforeFile +
                "\nrootBindings=" + RootBeforeFile +
                "\nrootAssetHashes=" + RootAssetsBeforeFile +
                "\nmethod=Deactivate only the named owned root in temporary Play, capture before_reconstructed, restore active state before leaving Play.\n" +
                "\nprovenance=Reconstructed comparison under new UI code; not an unmodified original pre-change image.\n",
                Encoding.UTF8);
            return "Post-UI checkpoint captured without replacing original baseline: " + CurrentRecord +
                "; original scene and mesh/material assets verified, candidate01 root 14 dedicated assets snapshotted.";
        }

        // Call in clean Edit mode immediately before authoring. The before screenshot is
        // captured separately by StartPlay("before") using the same live Game fixture.
        public static string SnapshotBefore()
        {
            if (IsPostUiCheckpoint) throw new InvalidOperationException("Original SnapshotBefore is first-round only. Use SnapshotPostUiCheckpoint.");
            RequireCleanEdit();
            var mapEdit = UnityEngine.Object.FindFirstObjectByType<StageABlockoutMap>();
            if (!mapEdit || mapEdit.gameObject.scene.path != StageV08Build.Scene)
                throw new InvalidOperationException("Retained A map is not open.");
            if (FindAddedRoot(mapEdit.transform))
                throw new InvalidOperationException("New trial root already exists; no true before snapshot possible.");
            string inventory = Path.Combine(CurrentRecord, BaselineFile);
            string protectedFile = Path.Combine(CurrentRecord, ProtectedBeforeFile);
            string assetsFile = Path.Combine(CurrentRecord, AssetsBeforeFile);
            RefuseExisting(inventory); RefuseExisting(protectedFile); RefuseExisting(assetsFile);
            Directory.CreateDirectory(Path.GetDirectoryName(inventory));
            File.WriteAllLines(inventory, Inventory(mapEdit.gameObject.scene, mapEdit.transform), Encoding.UTF8);
            File.WriteAllLines(protectedFile, ProtectedHashes(), Encoding.UTF8);
            File.WriteAllLines(assetsFile, ReferencedAssetHashes(mapEdit.gameObject.scene, mapEdit.transform), Encoding.UTF8);
            return "True pre-change scene inventory, referenced material/mesh hashes, and four protected source hashes saved. Run StartPlay(\"before\") before authoring.";
        }

        // After authoring, compare without mutating or saving the scene. This is a strict
        // exact comparison of pre-existing objects, not a gameplay visual verdict.
        public static string CompareAfter()
        { return CompareAfterCore("AFTER_PRESERVATION.txt"); }

        // For a revised visual candidate. It reuses the immutable true-before inventory
        // and never overwrites the first after report or another candidate report.
        public static string CompareAfterTagged(string candidateTag)
        { return CompareAfterCore("AFTER_PRESERVATION_" + SafeCandidateTag(candidateTag) + ".txt"); }

        // Run while the edited A scene is still dirty, before the owner saves it.
        // The immutable true-before snapshot is reused; the only output is this report.
        public static string CompareWorkingCopy(string candidateTag)
        { return CompareAfterCore("PRE_SAVE_PRESERVATION_" + SafeCandidateTag(candidateTag) + ".txt", false); }

        static string CompareAfterCore(string reportName, bool requireClean = true)
        {
            if (requireClean) RequireCleanEdit(); else RequireStableEdit();
            string baseline = Path.Combine(CurrentRecord, BaselineFile);
            string protectedFile = Path.Combine(CurrentRecord, ProtectedBeforeFile);
            string assetsFile = Path.Combine(CurrentRecord, AssetsBeforeFile);
            if (!File.Exists(baseline) || !File.Exists(protectedFile) || !File.Exists(assetsFile))
                throw new FileNotFoundException("True pre-change inventory, referenced asset, and protected source hashes required.");
            var mapEdit = UnityEngine.Object.FindFirstObjectByType<StageABlockoutMap>();
            if (!mapEdit || mapEdit.gameObject.scene.path != StageV08Build.Scene)
                throw new InvalidOperationException("Retained A map is not open.");
            var root = FindAddedRoot(mapEdit.transform);
            if (!root) throw new InvalidOperationException("Expected new visual root is absent: " + AddedRootName);
            string compareFile = Path.Combine(CurrentRecord, "staging/validation_tool/" + reportName);
            RefuseExisting(compareFile);
            var before = File.ReadAllLines(baseline).Skip(1).ToArray();
            var after = Inventory(mapEdit.gameObject.scene, mapEdit.transform).Skip(1).ToArray();
            var previous = new HashSet<string>(before, StringComparer.Ordinal);
            var present = new HashSet<string>(after, StringComparer.Ordinal);
            string[] missingOrModified = previous.Except(present).Take(100).ToArray();
            string[] addedOutsideRoot = present.Except(previous).Take(100).ToArray();
            // Exact scene rows include each old renderer's material/mesh asset GUID
            // and path. If even one row differs, the frozen referenced-asset path list
            // cannot be reused safely; fail before any expensive asset hashing.
            bool inventorySame = before.Length == after.Length && before.SequenceEqual(after);
            if (!inventorySame)
            {
                var mismatch = new StringBuilder("STAGE_A_CLINIC_MOOD_TRIAL_PRESERVATION\n");
                mismatch.AppendLine("result=FAIL");
                mismatch.AppendLine("reason=existing_scene_inventory_changed; frozen referenced asset list not reused");
                mismatch.AppendLine("existingObjectRowsBefore=" + before.Length + " after=" + after.Length);
                mismatch.AppendLine("existingMissingOrModified=" + previous.Except(present).Count() +
                    " outsideRootAdded=" + present.Except(previous).Count());
                foreach (string line in missingOrModified) mismatch.AppendLine("MISSING_OR_MODIFIED " + line);
                foreach (string line in addedOutsideRoot) mismatch.AppendLine("ADDED_OUTSIDE_ROOT " + line);
                mismatch.AppendLine("next=Investigate old-object edits. Recollect dependencies only through a separately reviewed new baseline; do not silently refresh this before snapshot.");
                File.WriteAllText(compareFile, mismatch.ToString(), Encoding.UTF8);
                return "FAIL: " + compareFile;
            }
            string[] protectedBefore = File.ReadAllLines(protectedFile);
            string[] protectedAfter = ProtectedHashes();
            bool protectedSame = protectedBefore.SequenceEqual(protectedAfter);
            string[] assetsBefore = File.ReadAllLines(assetsFile);
            string[] assetsAfter = RehashFrozenAssets(assetsBefore, out string assetBaselineIssue);
            bool assetsSame = assetBaselineIssue == null && assetsBefore.SequenceEqual(assetsAfter);
            var colliders = root.GetComponentsInChildren<Collider>(true);
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var lights = root.GetComponentsInChildren<Light>(true);
            int addedTriangles = renderers.Sum(TriangleCount);
            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            int addedVertices = filters.Sum(f => f.sharedMesh ? f.sharedMesh.vertexCount : 0);
            bool dedicatedAssets = renderers.All(r => r.sharedMaterials.All(m =>
                m && AssetDatabase.GetAssetPath(m).StartsWith(AddedAssetPrefix, StringComparison.Ordinal))) &&
                filters.All(f => f.sharedMesh &&
                    AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith(AddedAssetPrefix, StringComparison.Ordinal));
            var facade = root.Find("GrowthCanopy");
            var marker = facade ? facade.GetComponent<SurfaceIvyInstance>() : null;
            var binding = facade ? facade.GetComponent<AMapRuinIvyRevealBinding>() : null;
            bool revealBinding = marker && binding && marker.sourceRenderers != null &&
                marker.sourceRenderers.Length == 1 && marker.sourceRenderers[0] &&
                !marker.sourceRenderers[0].transform.IsChildOf(root);
            bool valid = missingOrModified.Length == 0 && addedOutsideRoot.Length == 0 &&
                protectedSame && assetsSame && colliders.Length == 0 && lights.Length == 0 &&
                renderers.Length == 6 && addedTriangles <= MaxAddedTriangles &&
                addedVertices <= MaxAddedVertices && dedicatedAssets && revealBinding;
            var sb = new StringBuilder();
            sb.AppendLine("STAGE_A_CLINIC_MOOD_TRIAL_PRESERVATION");
            sb.AppendLine("savedSceneAtInspection=" + (!mapEdit.gameObject.scene.isDirty));
            sb.AppendLine("preSaveAudit=" + (!requireClean));
            sb.AppendLine("result=" + (valid ? "PASS_STATIC_ONLY" : "FAIL"));
            sb.AppendLine("newRoot=" + PathOf(root) + " renderers=" + renderers.Length +
                " triangles=" + addedTriangles + "/" + MaxAddedTriangles +
                " vertices=" + addedVertices + "/" + MaxAddedVertices +
                " colliders=" + colliders.Length + " lights=" + lights.Length);
            sb.AppendLine("dedicatedMaterialAndMeshAssets=" + dedicatedAssets + " revealBindingToOriginalSource=" + revealBinding);
            sb.AppendLine("existingObjectRowsBefore=" + before.Length + " after=" + after.Length);
            sb.AppendLine("existingMissingOrModified=" + previous.Except(present).Count() +
                " outsideRootAdded=" + present.Except(previous).Count());
            foreach (var line in missingOrModified) sb.AppendLine("MISSING_OR_MODIFIED " + line);
            foreach (var line in addedOutsideRoot) sb.AppendLine("ADDED_OUTSIDE_ROOT " + line);
            sb.AppendLine("protectedFilesSame=" + protectedSame);
            for (int i = 0; i < protectedBefore.Length; i++)
                if (i >= protectedAfter.Length || protectedBefore[i] != protectedAfter[i])
                    sb.AppendLine("PROTECTED_CHANGED " + protectedBefore[i] + " -> " +
                        (i < protectedAfter.Length ? protectedAfter[i] : "MISSING"));
            sb.AppendLine("existingReferencedAssetsSame=" + assetsSame + " beforeCount=" + assetsBefore.Length +
                " afterCount=" + assetsAfter.Length + " expectedCount=" + ExpectedReferencedAssetCount);
            sb.AppendLine("frozenAssetBaselineIssue=" + (assetBaselineIssue ?? "none"));
            sb.AppendLine("assetScan=Rehashed only frozen before paths; did not call AssetDatabase.GetDependencies after exact old-scene inventory match.");
            foreach (string asset in assetsBefore.Except(assetsAfter).Take(100)) sb.AppendLine("REFERENCED_ASSET_CHANGED " + asset);
            foreach (var collider in colliders) sb.AppendLine("NEW_COLLIDER " + PathOf(collider.transform));
            sb.AppendLine("scope=All retained A scene Edit hierarchies excluding only CLINIC.VisualRoot named new visual root; source hashes are bytes on disk.");
            sb.AppendLine("runtime material instances are intentionally not compared; authored shared asset GUID/path is compared.");
            sb.AppendLine("triangle/vertex caps are scope gates, not standalone GPU/FPS performance PASS.");
            File.WriteAllText(compareFile, sb.ToString(), Encoding.UTF8);
            return (valid ? "PASS_STATIC_ONLY" : "FAIL") + ": " + compareFile;
        }

        // Call after both Play sessions have returned to Edit. This checks fixture
        // equivalence and reports performance deltas, without granting visual PASS.
        public static string ComparePlayPairs()
        { return ComparePlayPairsCore("after", "PLAY_PAIR_COMPARISON.txt"); }

        public static string ComparePlayPairsAfter(string candidateTag)
        {
            string tag = SafeCandidateTag(candidateTag);
            return ComparePlayPairsCore("after_" + tag, "PLAY_PAIR_COMPARISON_" + tag + ".txt");
        }

        static string ComparePlayPairsCore(string afterPhase, string resultName)
        {
            RequireCleanEdit();
            string beforePhase = IsPostUiCheckpoint ? "before_reconstructed" : "before";
            string beforePath = Path.Combine(CurrentRecord, "staging/validation_tool/play_" + beforePhase + "/PLAY_REPORT.json");
            string afterPath = Path.Combine(CurrentRecord, "staging/validation_tool/play_" + afterPhase + "/PLAY_REPORT.json");
            if (!File.Exists(beforePath) || !File.Exists(afterPath))
                throw new FileNotFoundException("Both actual Play reports are required.");
            var before = JsonUtility.FromJson<Report>(File.ReadAllText(beforePath));
            var after = JsonUtility.FromJson<Report>(File.ReadAllText(afterPath));
            string resultPath = Path.Combine(CurrentRecord, "staging/validation_tool/" + resultName);
            RefuseExisting(resultPath);
            var sb = new StringBuilder("CLINIC_MOOD_TRIAL_PLAY_PAIR\n");
            string[] expectedIds = ViewIds();
            const string CompleteStatus = "CAPTURED_STATIC_AND_MOVEMENT_REVIEW_REQUIRED";
            bool fixture = before.opening == after.opening && before.width == after.width &&
                before.height == after.height && before.width == Width && before.height == Height &&
                before.seed == after.seed && before.phase == beforePhase && after.phase == afterPhase &&
                before.comparisonContext == after.comparisonContext &&
                before.environmentFingerprint == after.environmentFingerprint &&
                before.status == CompleteStatus && after.status == CompleteStatus &&
                before.movementStatus == "PASS_ACTUAL_PLAYER_MOTOR" &&
                after.movementStatus == "PASS_ACTUAL_PLAYER_MOTOR" &&
                before.viewCount == expectedIds.Length && after.viewCount == expectedIds.Length &&
                before.views != null && after.views != null &&
                before.views.Length == expectedIds.Length && after.views.Length == expectedIds.Length;
            if (IsPostUiCheckpoint)
                fixture &= before.reconstructedBefore && before.rootHiddenDuringCapture &&
                    before.rootWasActive && before.rootStateRestored &&
                    !string.IsNullOrEmpty(before.hiddenRootPath) &&
                    before.rootAssetSnapshot == RootAssetsBeforeFile && !after.reconstructedBefore;
            if (afterPhase == "after_candidate02_hudmatched")
            {
                fixture &= after.loadoutFingerprint == ExpectedHudFingerprint &&
                    after.operationTimerAtFreeze == "29:57" &&
                    after.views != null && after.views.All(v => v != null &&
                        v.loadoutFingerprint == ExpectedHudFingerprint && v.operationTimeText == "29:57");
                sb.AppendLine("afterHudLoadout=" + after.loadoutFingerprint +
                    " expected=" + ExpectedHudFingerprint);
                sb.AppendLine("afterOperationTimer=" + after.operationTimerAtFreeze + " expected=29:57");
                sb.AppendLine("beforeHudLoadout=not recorded in immutable reconstructed-before JSON; reviewed before PNG shows 30/70 and 19.8 kg. Manual image review remains required.");
            }
            sb.AppendLine("expectedViews=" + expectedIds.Length + " order=" + string.Join(",", expectedIds));
            sb.AppendLine("contextBefore=" + before.comparisonContext + " after=" + after.comparisonContext);
            sb.AppendLine("beforeReconstructed=" + before.reconstructedBefore +
                " ownedRootHidden=" + before.rootHiddenDuringCapture +
                " ownedRootRestored=" + before.rootStateRestored +
                " hiddenRoot=" + before.hiddenRootPath);
            sb.AppendLine("sessionStatusBefore=" + before.status + " after=" + after.status);
            sb.AppendLine("viewCountBefore=" + before.viewCount + " after=" + after.viewCount);
            sb.AppendLine("openingSame=" + (before.opening == after.opening));
            sb.AppendLine("resolutionFullHdBoth=" + (before.width == Width && before.height == Height &&
                after.width == Width && after.height == Height));
            sb.AppendLine("seedSame=" + (before.seed == after.seed));
            sb.AppendLine("environmentFingerprintSame=" + (before.environmentFingerprint == after.environmentFingerprint));
            sb.AppendLine("shaderTimeLimit=" + after.shaderTimeLimit);
            sb.AppendLine("beforeMovement=" + before.movementStatus + " afterMovement=" + after.movementStatus);
            sb.AppendLine("beforeRootRenderers=" + before.addedRenderers + " afterRootRenderers=" + after.addedRenderers);
            sb.AppendLine("beforeRootTriangles=" + before.addedTriangles + " afterRootTriangles=" + after.addedTriangles);
            if (before.views != null && after.views != null)
            for (int i = 0; i < Math.Min(before.views.Length, after.views.Length); i++)
            {
                View a = before.views[i], b = after.views[i];
                bool order = i < expectedIds.Length && a.id == expectedIds[i] && b.id == expectedIds[i];
                bool pose = order && a.player == b.player && a.eye == b.eye &&
                    a.navSampleSource == b.navSampleSource &&
                    a.target == b.target && Mathf.Approximately(a.fov, b.fov);
                fixture &= pose && a.gameExists && b.gameExists && a.cameraExists && b.cameraExists &&
                    a.status == "CAPTURED_REVIEW_REQUIRED" && b.status == "CAPTURED_REVIEW_REQUIRED" &&
                    a.samples == SampleFrames && b.samples == SampleFrames;
                sb.AppendLine(a.id + " expectedOrder=" + order + " poseSame=" + pose +
                    " navSource=" + a.navSampleSource + "->" + b.navSampleSource +
                    " samples=" + a.samples + "->" + b.samples +
                    " before=" + a.status + " after=" + b.status +
                    " newRootActive=" + a.rootActiveRenderers + "->" + b.rootActiveRenderers +
                    " newRootShadows=" + a.rootShadowCasters + "->" + b.rootShadowCasters +
                    " reveal=" + a.revealActive + "->" + b.revealActive +
                    " editorP95Ms=" + a.editorP95Ms.ToString("F3") + "->" + b.editorP95Ms.ToString("F3") +
                    " gpuMedianMs=" + a.frameGpuMedianMs.ToString("F3") + "->" + b.frameGpuMedianMs.ToString("F3") +
                    " drawsMedian=" + a.drawCallsMedian.ToString("F1") + "->" + b.drawCallsMedian.ToString("F1") +
                    " sceneTrianglesMedian=" + a.sceneTrianglesMedian.ToString("F1") + "->" + b.sceneTrianglesMedian.ToString("F1"));
            }
            sb.AppendLine("fixtureResult=" + (fixture ? "MATCHED_REVIEW_REQUIRED" : "MISMATCH"));
            sb.AppendLine("Performance samples are Editor Play only; root renderer/triangle counts are scoped to the new root; UnityStats covers the entire Game view.");
            sb.AppendLine("Inspect every PNG pair, including roots/ivy/window/entrance/shadow/interior and remote controls. No automatic visual or standalone FPS PASS.");
            File.WriteAllText(resultPath, sb.ToString(), Encoding.UTF8);
            return (fixture ? "MATCHED_REVIEW_REQUIRED" : "MISMATCH") + ": " + resultPath;
        }

        public static string StartPlay(string requestedPhase)
        {
            if (requestedPhase != "before" && requestedPhase != "after")
                throw new ArgumentException("Use before or after", nameof(requestedPhase));
            return StartPlayCore(requestedPhase);
        }

        public static string StartPostUiReconstructedBefore()
        {
            RequirePostUiCheckpoint();
            return StartPlayCore("before_reconstructed");
        }

        public static string StartPlayAfter(string candidateTag)
        { return StartPlayCore("after_" + SafeCandidateTag(candidateTag)); }

        public static string StartRevealSupplement()
        {
            RequirePostUiCheckpoint();
            return StartPlayCore("sr_candidate02_reveal");
        }

        static bool IsHudFixturePhase(string requestedPhase) =>
            requestedPhase == "after_candidate02_hudmatched" ||
            requestedPhase == "sr_candidate02_reveal" ||
            requestedPhase == "supplement_candidate02_reveal";
        static bool IsRevealSupplementPhase(string requestedPhase) =>
            requestedPhase == "sr_candidate02_reveal" || requestedPhase == "supplement_candidate02_reveal";
        static string[] RevealSupplementViewIds() => new[] {
            "clinic_north_reveal", "clinic_north_reveal_restored" };

        static string StartPlayCore(string requestedPhase)
        {
            RequireCleanEdit();
            if (IsPostUiCheckpoint && requestedPhase == "before")
                throw new InvalidOperationException("Post-UI before must be labelled before_reconstructed.");
            if (!IsPostUiCheckpoint && requestedPhase == "before_reconstructed")
                throw new InvalidOperationException("Reconstructed before is post-UI checkpoint only.");
            if (IsRevealSupplementPhase(requestedPhase) && !IsPostUiCheckpoint)
                throw new InvalidOperationException("Reveal supplement is post-UI checkpoint only.");
            if (!File.Exists(Path.Combine(CurrentRecord, BaselineFile)))
                throw new InvalidOperationException("SnapshotBefore must precede Play capture.");
            if (requestedPhase == "before_reconstructed") ValidateReconstructionSource();
            if (requestedPhase.StartsWith("after", StringComparison.Ordinal) &&
                !File.Exists(Path.Combine(CurrentRecord, IsPostUiCheckpoint
                    ? "before_reconstructed_clinic_true_exterior.png" : "before_clinic_true_exterior.png")))
                throw new InvalidOperationException("Real before Game image required before after capture.");
            if (IsRevealSupplementPhase(requestedPhase) &&
                !File.Exists(Path.Combine(CurrentRecord,
                    "staging/validation_tool/play_after_candidate02_hudmatched/PLAY_REPORT.json")))
                throw new InvalidOperationException("HUD-matched ten-view report required before reveal supplement.");
            string folder = Path.Combine(CurrentRecord, "staging/validation_tool/play_" + requestedPhase);
            if (Directory.Exists(folder) && Directory.EnumerateFileSystemEntries(folder).Any())
                throw new IOException("Existing Play report is preserved: " + folder);
            foreach (string id in IsRevealSupplementPhase(requestedPhase) ? RevealSupplementViewIds() : ViewIds())
                RefuseExisting(Path.Combine(CurrentRecord, requestedPhase + "_" + id + ".png"));
            Directory.CreateDirectory(folder);
            phase = requestedPhase; output = folder;
            SessionState.SetBool(Key, true);
            SessionState.SetString(Key + ".Phase", phase);
            SessionState.SetString(Key + ".Started", DateTime.UtcNow.Ticks.ToString());
            oldBackground = Application.runInBackground;
            SessionState.SetBool(Key + ".Background", oldBackground);
            Application.runInBackground = true;
            EditorApplication.isPaused = false;
            EditorApplication.isPlaying = true;
            return "Controlled actual Game capture started: " + phase + ". No scene save is performed.";
        }

        public static string Status() => "active=" + SessionState.GetBool(Key, false) +
            " phase=" + phase + " stage=" + stage + " view=" + index + " frame=" + frames;

        public static string Cancel()
        {
            if (SessionState.GetBool(Key, false)) Finish("Cancelled explicitly; partial evidence retained.");
            return "Cancelled";
        }

        static void Poll()
        {
            if (!SessionState.GetBool(Key, false)) return;
            try
            {
                if (DateTime.UtcNow - new DateTime(long.Parse(SessionState.GetString(Key + ".Started", "0")), DateTimeKind.Utc) > TimeSpan.FromMinutes(12))
                    throw new TimeoutException("Clinic trial capture timed out.");
                if (!EditorApplication.isPlaying || EditorApplication.isCompiling || Time.frameCount == lastFrame) return;
                lastFrame = Time.frameCount;
                if (!initialized) { Initialize(); return; }
                if (stage == 1) { Settle(); return; }
                if (stage == 2) { Sample(); return; }
                if (stage == 3) { Confirm(); return; }
                if (stage == 4) { Walk(); return; }
            }
            catch (Exception error) { Finish(error.ToString()); }
        }

        static void Initialize()
        {
            BlockLiveInputEarly();
            if (Time.frameCount < Warmup) return;
            phase = SessionState.GetString(Key + ".Phase", "unknown");
            output = Path.Combine(CurrentRecord, "staging/validation_tool/play_" + phase);
            preview = UnityEngine.Object.FindFirstObjectByType<StageScenePreview>();
            if (!preview || !preview.World || !preview.World.MapRouter || !preview.World.MapRouter.IsStageA) return;
            var world = preview.World;
            if (world.Store == null || string.IsNullOrWhiteSpace(world.Store.Path) ||
                world.Inventory == null || !world.Inventory.Player) return;
            if (IsHudFixturePhase(phase))
            {
                if (world.OperationEndsAt < 0) return;
                double remaining = world.OperationRemaining(Time.timeAsDouble);
                if (remaining > 1797d) return; // The reviewed before HUD shows 29:57.
                if (remaining <= 1796d)
                    throw new InvalidOperationException("HUD timer missed the 29:57 fixture window; remaining=" + remaining.ToString("F3", CultureInfo.InvariantCulture));
            }
            map = world.MapRouter.Map; player = world.Inventory.Player;
            inventory = world.Inventory; rifle = inventory.Rifle;
            body = player.GetComponent<CharacterController>(); camera = player.view;
            follow = camera ? camera.GetComponent<TopDownCamera>() : null;
            if (!map || !body || !camera || !follow)
                throw new InvalidOperationException("Live map/player/CharacterController/camera unavailable.");
            string store = Path.GetFullPath(world.Store.Path);
            string temp = Path.GetFullPath(Path.Combine(Application.temporaryCachePath, "StageMapPreview")) + Path.DirectorySeparatorChar;
            if (!store.StartsWith(temp, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only temporary StageMapPreview store accepted.");
            var clinic = StageABuildingD04Validation.CollectSites(map).Single(s => s.Id == "CLINIC");
            opening = StageABuildingD04Validation.FindActualOpenings(map, clinic).FirstOrDefault();
            if (opening == null) throw new InvalidOperationException("Real CLINIC entrance opening missing.");
            var added = FindAddedRoot(map.transform);
            if (phase == "before" && added) throw new InvalidOperationException("New visual root present during true before capture.");
            if (phase != "before" && !added) throw new InvalidOperationException("New visual root missing during after capture.");
            report = new Report {
                phase = phase, status = "RUNNING", startedUtc = DateTime.UtcNow.ToString("O"),
                scene = map.gameObject.scene.path, store = store, temporaryStore = true,
                liveStageA = world.MapRouter.IsStageA, currentSceneClean = !SceneManager.GetActiveScene().isDirty,
                sceneWasSaved = false, width = Width, height = Height, seed = Seed,
                rootName = AddedRootName,
                opening = opening.Label + " outside=" + opening.Outside.ToString("F4") +
                    " inside=" + opening.Inside.ToString("F4"),
                rendererScope = "new visual root renderer/triangle count versus whole Game-view UnityStats sample",
                timingScope = "Editor Play sample only; not standalone build FPS or GPU budget certification",
                environmentFingerprint = EnvironmentFingerprint(),
                shaderTimeLimit = "Time.timeScale=0 after runtime warmup; Unity shader _Time/unscaled effects and load-time weather phases are not proven identical by seed alone",
                comparisonContext = IsPostUiCheckpoint ? PostUiSubfolder : "original_trial",
                reconstructedBefore = phase == "before_reconstructed",
                reconstructionBasis = phase == "before_reconstructed"
                    ? "Post-UI checkpoint: original scene/material/mesh hashes verified in Edit; reviewed candidate01-owned root hidden only in temporary Play"
                    : "not_reconstructed",
                rootAssetSnapshot = phase == "before_reconstructed" ? RootAssetsBeforeFile : "none"
            };
            if (IsHudFixturePhase(phase)) report.operationTimerAtFreeze = world.OperationTimeText(Time.timeAsDouble);
            if (added)
            {
                report.addedColliders = added.GetComponentsInChildren<Collider>(true).Length;
                report.addedRenderers = added.GetComponentsInChildren<Renderer>(true).Length;
                report.addedTriangles = added.GetComponentsInChildren<Renderer>(true).Sum(TriangleCount);
            }
            oldWorldTicks = world.UseAutomaticTicks;
            oldInput = earlyInputPlayer == player ? earlyInputWasLive : player.UseLiveInput;
            oldFollow = follow.enabled; oldPlayer = player.transform.position;
            oldPlayerRotation = player.transform.rotation; oldCamera = camera.transform.position;
            oldCameraRotation = camera.transform.rotation; oldFov = camera.fieldOfView;
            oldTimeScale = Time.timeScale; oldRandom = UnityEngine.Random.state;
            randomCaptured = savedState = true;
            UnityEngine.Random.InitState(Seed);
            world.UseAutomaticTicks = false; player.UseLiveInput = false; follow.enabled = false;
            if (IsHudFixturePhase(phase)) EnsureHudFixture();
            Time.timeScale = 0f;
            foreach (var enemy in EnemyController.ActiveEnemies.Where(e => e && e.IsEncounterActor && e.isActiveAndEnabled))
            { enemies[enemy] = (enemy.Frozen, enemy.UseAutomaticTicks); enemy.Frozen = true; enemy.UseAutomaticTicks = false; }
            if (phase == "before_reconstructed")
            {
                reconstructionRoot = added;
                reconstructionRootWasActive = added.gameObject.activeSelf;
                if (!reconstructionRootWasActive || added.GetComponentsInChildren<Renderer>(true).Length != 6)
                    throw new InvalidOperationException("Reviewed candidate01 root is not active with six generated renderers.");
                report.rootWasActive = true;
                report.hiddenRootPath = PathOf(added);
                added.gameObject.SetActive(false);
                Physics.SyncTransforms();
                if (added.GetComponentsInChildren<Renderer>(true).Any(r => r.enabled && r.gameObject.activeInHierarchy && !r.forceRenderingOff))
                    throw new InvalidOperationException("One of six generated renderers remained active after owned-root hide.");
                report.rootHiddenDuringCapture = true;
            }
            poses = BuildPoses(clinic.Center.position, opening);
            initialized = true; index = 0; SetupPose();
        }

        static void BlockLiveInputEarly()
        {
            string requested = SessionState.GetString(Key + ".Phase", "");
            if (!IsHudFixturePhase(requested) || earlyInputPlayer) return;
            var scenePreview = UnityEngine.Object.FindFirstObjectByType<StageScenePreview>();
            var world = scenePreview ? scenePreview.World : null;
            var motor = world != null && world.MapRouter != null && world.MapRouter.IsStageA &&
                world.Inventory != null ? world.Inventory.Player : null;
            if (!motor) return;
            earlyInputPlayer = motor; earlyInputWasLive = motor.UseLiveInput;
            motor.UseLiveInput = false;
        }

        static void EnsureHudFixture()
        {
            if (!inventory || inventory.Bag == null || !rifle || rifle.Active == null)
                throw new InvalidOperationException("Live inventory/rifle missing for HUD-matched fixture.");
            report.loadoutBeforeFixture = HudLoadoutFingerprint();
            RifleState active = rifle.Active;
            string method = "already_loaded";
            if (active.Magazine == 0 && !active.Reloading)
            {
                bool accepted = rifle.TryAutomaticReload();
                if (!accepted) accepted = rifle.Reload();
                if (!accepted) throw new InvalidOperationException("Normal rifle reload could not begin; " + report.loadoutBeforeFixture);
                method = "normal_reload_started";
            }
            if (active.Reloading)
            {
                rifle.Tick(Math.Max(rifle.Clock, active.ReloadEndsAt) + .01, false);
                method = method == "already_loaded" ? "existing_reload_completed_by_tick" :
                    "normal_reload_completed_by_tick";
            }
            if (!HudLoadoutMatchesExpected())
                throw new InvalidOperationException("HUD loadout differs from reviewed before 30/70 and 19.8 kg: " + HudLoadoutFingerprint());
            report.loadoutFingerprint = HudLoadoutFingerprint();
            report.loadoutFixtureMethod = method + "; Play-only existing RifleController/RifleState timed reload; no production save";
        }

        static bool HudLoadoutMatchesExpected()
        {
            RifleState active = rifle ? rifle.Active : null;
            return active != null && inventory && inventory.Bag != null &&
                active.Definition.id == "AR_001" && active.Definition.ammoId == "item_018" &&
                active.Magazine == 30 && !active.Reloading &&
                AmmoRules.CountCompatible(active.Definition, inventory.Bag) == 70 &&
                Mathf.Abs(inventory.Bag.Weight - 19.8f) <= .05f;
        }

        static string HudLoadoutFingerprint()
        {
            RifleState active = rifle ? rifle.Active : null;
            if (active == null || !inventory || inventory.Bag == null) return "NO_LIVE_LOADOUT";
            return "weapon=" + active.Definition.id + " ammo=" + active.Definition.ammoId +
                " magazine=" + active.Magazine + " reserve=" +
                AmmoRules.CountCompatible(active.Definition, inventory.Bag) +
                " weightKg=" + inventory.Bag.Weight.ToString("F1", CultureInfo.InvariantCulture) +
                " reloading=" + active.Reloading;
        }

        static Pose[] BuildPoses(Vector3 center, StageABuildingD04Validation.Passage door)
        {
            if (IsRevealSupplementPhase(phase))
                return new[] {
                    new Pose { id="clinic_north_reveal", feet=new Vector3(-168.35f,4.04f,139.6f),
                        eye=new Vector3(-162f,11f,151f), target=new Vector3(-168.35f,5.3f,139.6f) },
                    new Pose { id="clinic_north_reveal_restored", feet=new Vector3(-168.35f,4.04f,145f),
                        eye=new Vector3(-162f,11f,151f), target=new Vector3(-168.35f,5.3f,141.95f) }
                };
            Vector3 outwards = Vector3.ProjectOnPlane(door.Outside - center, Vector3.up).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, outwards).normalized;
            return new[] {
                new Pose { id="clinic_gameplay", feet=new Vector3(-166,4.5981f,155), gameplayOrbit=true },
                new Pose { id="clinic_true_exterior", feet=new Vector3(-166,4.5981f,155),
                    eye=new Vector3(-158,17,167), target=new Vector3(-166,4,149) },
                new Pose { id="clinic_entrance", feet=door.Outside,
                    eye=door.Outside+outwards*2+side*3+Vector3.up*9,
                    target=door.Inside+Vector3.up*1.5f },
                new Pose { id="clinic_approach", feet=door.Outside+outwards*5,
                    eye=door.Outside+outwards*8+side*6+Vector3.up*11,
                    target=door.Outside+Vector3.up*2 },
                new Pose { id="clinic_north_seam_close", feet=new Vector3(-168.35f,4.04f,145f),
                    eye=new Vector3(-162f,11f,151f), target=new Vector3(-168.35f,5.3f,141.95f) },
                new Pose { id="clinic_outside_inside", feet=door.Outside,
                    eye=door.Outside+outwards*4+side*2+Vector3.up*7,
                    target=door.Inside+Vector3.up*1.4f },
                new Pose { id="clinic_inside_out", feet=door.Inside,
                    eye=door.Inside-outwards*3+side*2+Vector3.up*7,
                    target=door.Outside+Vector3.up*1.5f },
                new Pose { id="remote_w05_control", feet=new Vector3(-132,1.8f,9),
                    eye=new Vector3(-121,16.8f,-6), target=new Vector3(-132,2.8f,9) },
                new Pose { id="remote_bridge_control", feet=new Vector3(102.5f,.2f,-125),
                    eye=new Vector3(115.5f,19.2f,-146), target=new Vector3(102.5f,1.2f,-125) },
                new Pose { id="HALL1_control", feet=new Vector3(0,2,-42),
                    eye=new Vector3(14,19,-60), target=new Vector3(0,3,-42) }
            };
        }

        static string[] ViewIds() => new[] { "clinic_gameplay", "clinic_true_exterior", "clinic_entrance", "clinic_approach",
            "clinic_north_seam_close", "clinic_outside_inside", "clinic_inside_out",
            "remote_w05_control", "remote_bridge_control", "HALL1_control" };

        static void SetupPose()
        {
            if (index >= poses.Length) { BeginWalk(); return; }
            Pose pose = poses[index];
            current = new View { id = pose.id, status = "PENDING" };
            views.Add(current);
            current.requestedFeet = pose.feet.ToString("F4");
            NavMeshHit hit;
            bool found = TrySamplePose(pose.feet, out hit);
            if (found) current.navSampleSource = "original_pose_y";
            else
            {
                var landform = map.GetComponentInChildren<StageALandformV09>(true);
                if (!landform) throw new InvalidOperationException("Stage A terrain height source missing for fallback.");
                Vector3 fallback = pose.feet;
                fallback.y = landform.SampleMapHeight(fallback.x + 260f, 200f - fallback.z);
                found = TrySamplePose(fallback, out hit);
                current.navSampleSource = found ? "terrain_height_fallback" : "both_failed";
            }
            if (!found)
            { current.status = "POSE_FAILED"; current.error = "No close NavMesh ground at original Y or terrain fallback";
              index++; SetupPose(); return; }
            body.enabled = false;
            player.transform.SetPositionAndRotation(hit.position + Vector3.up * .04f, Quaternion.identity);
            body.enabled = true; Physics.SyncTransforms();
            Pin(); map.ConfigurePreviewVisibility(player.transform.position);
            current.player = player.transform.position.ToString("F4");
            current.eye = camera.transform.position.ToString("F4");
            current.target = pose.target.ToString("F4"); current.fov = camera.fieldOfView;
            var root = FindAddedRoot(map.transform);
            current.rootRenderers = root ? root.GetComponentsInChildren<Renderer>(true).Length : 0;
            current.rootTriangles = root ? root.GetComponentsInChildren<Renderer>(true).Sum(TriangleCount) : 0;
            editorMs.Clear(); cpuMs.Clear(); gpuMs.Clear(); draws.Clear(); triangles.Clear();
            frames = 0; stage = 1;
        }

        static bool TrySamplePose(Vector3 request, out NavMeshHit hit)
        {
            if (!NavMesh.SamplePosition(request, out hit, 2f, NavMesh.AllAreas)) return false;
            float horizontal = Vector2.Distance(new Vector2(hit.position.x, hit.position.z),
                new Vector2(request.x, request.z));
            float vertical = Mathf.Abs(hit.position.y - request.y);
            return horizontal <= 3f && vertical <= .85f;
        }

        static void Pin()
        {
            Pose p = poses[index];
            if (p.gameplayOrbit)
            {
                if (!follow || !follow.settings) throw new InvalidOperationException("Original TopDownCamera settings unavailable.");
                var settings = follow.settings;
                Vector3 focus = player.transform.position + Vector3.up;
                Vector3 offset = Quaternion.Euler(0, settings.cameraYaw, 0) *
                    Quaternion.Euler(-settings.cameraPitch, 0, 0) * Vector3.forward * settings.cameraDistance;
                offset = (offset + Vector3.up * settings.cameraHeight) * settings.worldScale;
                p.eye = focus + offset; p.target = focus;
                camera.transform.SetPositionAndRotation(p.eye, Quaternion.LookRotation(-offset));
            }
            else camera.transform.SetPositionAndRotation(p.eye, Quaternion.LookRotation(p.target - p.eye));
            camera.fieldOfView = follow && follow.settings ? follow.settings.fieldOfView : oldFov;
        }

        static void Settle()
        {
            player.Simulate(1f/60f, Vector3.zero, false, false, false);
            Pin(); map.ConfigurePreviewVisibility(player.transform.position);
            frames++;
            if (Screen.width != Width || Screen.height != Height)
            {
                if (frames >= ResolutionTimeoutFrames)
                    throw new InvalidOperationException("Current Game view is " + Screen.width + "x" + Screen.height +
                        "; maximized Full HD 1920x1080 is required. The tool did not change Game view selection.");
                return;
            }
            if (frames < SettleFrames) return;
            if (phase == "before_reconstructed" &&
                (!reconstructionRoot || reconstructionRoot.gameObject.activeSelf ||
                 reconstructionRoot.GetComponentsInChildren<Renderer>(true).Any(r => r.enabled && r.gameObject.activeInHierarchy && !r.forceRenderingOff)))
                throw new InvalidOperationException("Owned root became visible during reconstructed-before Game capture.");
            if (IsHudFixturePhase(phase) && !HudLoadoutMatchesExpected())
                throw new InvalidOperationException("HUD loadout drifted before Game capture: " + HudLoadoutFingerprint());
            if (IsHudFixturePhase(phase) && (!preview || !preview.World ||
                preview.World.OperationTimeText(Time.timeAsDouble) != "29:57"))
                throw new InvalidOperationException("HUD operation timer drifted before Game capture.");
            current.player = player.transform.position.ToString("F4");
            current.eye = camera.transform.position.ToString("F4");
            current.target = poses[index].target.ToString("F4");
            current.fov = camera.fieldOfView;
            current.loadoutFingerprint = IsHudFixturePhase(phase) ? HudLoadoutFingerprint() : "not_recorded";
            current.operationTimeText = IsHudFixturePhase(phase) ? preview.World.OperationTimeText(Time.timeAsDouble) : "not_recorded";
            var root = FindAddedRoot(map.transform);
            if (root)
            {
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                current.rootActiveRenderers = renderers.Count(r => r.enabled && r.gameObject.activeInHierarchy && !r.forceRenderingOff);
                current.rootShadowCasters = renderers.Count(r => r.enabled && r.gameObject.activeInHierarchy &&
                    r.shadowCastingMode != ShadowCastingMode.Off);
                current.rootRendererStates = string.Join(" | ", renderers.Select(r =>
                    PathOf(r.transform) + " enabled=" + r.enabled + " active=" + r.gameObject.activeInHierarchy +
                    " forcedOff=" + r.forceRenderingOff + " shadow=" + r.shadowCastingMode));
                Transform growth = root.Find("GrowthCanopy"), ground = root.Find("GroundAndDeposits");
                var growthRenderers = growth ? growth.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
                var groundRenderers = ground ? ground.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
                current.growthActiveRenderers = growthRenderers.Count(ActiveForCapture);
                current.groundActiveRenderers = groundRenderers.Count(ActiveForCapture);
                current.growthRendererStates = string.Join(" | ", growthRenderers.Select(r =>
                    r.name + " enabled=" + r.enabled + " active=" + r.gameObject.activeInHierarchy));
                current.groundRendererStates = string.Join(" | ", groundRenderers.Select(r =>
                    r.name + " enabled=" + r.enabled + " active=" + r.gameObject.activeInHierarchy));
                var ivy = growth ? growth.GetComponent<SurfaceIvyInstance>() : null;
                current.sourceShaderNames = ivy && ivy.sourceRenderers != null
                    ? string.Join(",", ivy.sourceRenderers.Where(r => r).SelectMany(r => r.sharedMaterials)
                        .Where(m => m && m.shader).Select(m => m.shader.name).Distinct().OrderBy(s => s, StringComparer.Ordinal))
                    : "NO_GROWTH_SOURCE";
            }
            var reveal = map.GetComponent<StageABuildingV33Reveal>();
            current.revealActive = reveal && reveal.IsRevealing;
            current.revealActiveRenderers = reveal ? reveal.ActiveRendererCount : -1;
            current.revealBlockingSurface = reveal ? reveal.BlockingSurfaceName : "NO_V33_REVEAL";
            current.cameraImage = Path.Combine(output, phase + "_camera_" + current.id + ".png");
            current.gameImage = Path.Combine(CurrentRecord, phase + "_" + current.id + ".png");
            StageABlockoutBuild.Capture(camera, current.cameraImage);
            ScreenCapture.CaptureScreenshot(current.gameImage, 1);
            frames = 0; stage = 2;
        }

        static bool ActiveForCapture(Renderer r) =>
            r && r.enabled && r.gameObject.activeInHierarchy && !r.forceRenderingOff;

        static void Sample()
        {
            player.Simulate(1f/60f, Vector3.zero, false, false, false);
            Pin();
            FrameTimingManager.CaptureFrameTimings();
            editorMs.Add(Time.unscaledDeltaTime * 1000f);
            if (FrameTimingManager.GetLatestTimings(1, timing) > 0)
            { if (timing[0].cpuFrameTime > 0) cpuMs.Add((float)timing[0].cpuFrameTime);
              if (timing[0].gpuFrameTime > 0) gpuMs.Add((float)timing[0].gpuFrameTime); }
            draws.Add(UnityStats.drawCalls); triangles.Add(UnityStats.triangles);
            if (++frames < SampleFrames) return;
            current.samples = frames;
            current.editorMedianMs = Percentile(editorMs, .5f); current.editorP95Ms = Percentile(editorMs, .95f);
            current.frameCpuMedianMs = Percentile(cpuMs, .5f); current.frameGpuMedianMs = Percentile(gpuMs, .5f);
            current.drawCallsMedian = Percentile(draws, .5f); current.sceneTrianglesMedian = Percentile(triangles, .5f);
            frames = 0; stage = 3;
        }

        static void Confirm()
        {
            if (++frames < 4) return;
            current.gameExists = File.Exists(current.gameImage);
            current.cameraExists = File.Exists(current.cameraImage);
            current.status = current.gameExists && current.cameraExists ? "CAPTURED_REVIEW_REQUIRED" : "MISSING_IMAGE";
            index++; SetupPose();
        }

        static void BeginWalk()
        {
            try
            {
                Vector3[] points = { opening.Outside, opening.Inside };
                var sampled = points.Select(p => {
                    if (!NavMesh.SamplePosition(p, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
                        throw new InvalidOperationException("Entrance NavMesh sample failed at " + p);
                    return hit.position;
                }).ToArray();
                var path = new NavMeshPath();
                if (!NavMesh.CalculatePath(sampled[0], sampled[1], NavMesh.AllAreas, path) ||
                    path.status != NavMeshPathStatus.PathComplete || path.corners.Length < 2)
                    throw new InvalidOperationException("Entrance NavMesh path incomplete: " + path.status);
                walkCorners = path.corners;
                body.enabled = false; player.transform.SetPositionAndRotation(sampled[0] + Vector3.up*.04f, Quaternion.identity);
                body.enabled = true; Physics.SyncTransforms();
                report.movementStart = player.transform.position.ToString("F4");
                walkTarget = 1; walkSteps = 0; stage = 4;
            }
            catch (Exception error) { report.movementStatus = "FAIL: " + error.Message; Finish(null); }
        }

        static void Walk()
        {
            for (int sub = 0; sub < 8; sub++)
            {
                Vector3 at = player.transform.position;
                Vector3 delta = Vector3.ProjectOnPlane(walkCorners[walkTarget] - at, Vector3.up);
                if (delta.magnitude < .6f)
                {
                    if (walkTarget < walkCorners.Length - 1) { walkTarget++; continue; }
                    float vertical = Mathf.Abs(at.y - walkCorners[walkCorners.Length - 1].y);
                    report.movementEnd = at.ToString("F4"); report.walkSteps = walkSteps;
                    report.movementStatus = vertical <= .85f ? "PASS_ACTUAL_PLAYER_MOTOR" : "FAIL_WRONG_ELEVATION";
                    Finish(null); return;
                }
                if (++walkSteps > 1500) { report.movementStatus = "FAIL_BLOCKED near " + at.ToString("F4"); Finish(null); return; }
                player.Simulate(1f/30f, delta.normalized, false, false, false);
            }
        }

        static void Finish(string error)
        {
            if (!SessionState.GetBool(Key, false)) return;
            SessionState.SetBool(Key, false);
            if (report == null) report = new Report { phase = SessionState.GetString(Key + ".Phase", "unknown") };
            report.error = error; report.finishedUtc = DateTime.UtcNow.ToString("O");
            report.views = views.ToArray(); report.viewCount = views.Count;
            try
            {
                if (reconstructionRoot)
                {
                    reconstructionRoot.gameObject.SetActive(reconstructionRootWasActive);
                    Physics.SyncTransforms();
                    report.rootStateRestored = reconstructionRoot.gameObject.activeSelf == reconstructionRootWasActive &&
                        reconstructionRoot.GetComponentsInChildren<Renderer>(true).Length == 6;
                }
                if (savedState && preview && preview.World) preview.World.UseAutomaticTicks = oldWorldTicks;
                foreach (var e in enemies) if (e.Key) { e.Key.Frozen = e.Value.frozen; e.Key.UseAutomaticTicks = e.Value.ticks; }
                if (savedState && player)
                { player.UseLiveInput = oldInput; if (body) body.enabled = false;
                  player.transform.SetPositionAndRotation(oldPlayer, oldPlayerRotation); if (body) body.enabled = true; }
                if (earlyInputPlayer) earlyInputPlayer.UseLiveInput = earlyInputWasLive;
                if (savedState && camera) { camera.transform.SetPositionAndRotation(oldCamera, oldCameraRotation); camera.fieldOfView = oldFov; }
                if (savedState && follow) follow.enabled = oldFollow;
                if (savedState) Time.timeScale = oldTimeScale;
                if (randomCaptured) UnityEngine.Random.state = oldRandom;
                report.randomStateRestored = randomCaptured; report.originalStateRestored = savedState;
            }
            catch (Exception restore) { report.error = (report.error ?? "") + " RESTORE: " + restore; report.status = "RESTORE_FAILED"; }
            if (IsRevealSupplementPhase(phase) && views.Count == 2)
            {
                View revealView = views[0], restoredView = views[1];
                bool first = revealView.id == "clinic_north_reveal" && revealView.revealActive &&
                    (revealView.sourceShaderNames ?? "").Contains("Hidden/Escape/BuildingV33LocalReveal") &&
                    revealView.growthActiveRenderers == 0 && revealView.groundActiveRenderers == 3;
                bool second = restoredView.id == "clinic_north_reveal_restored" && !restoredView.revealActive &&
                    !(restoredView.sourceShaderNames ?? "").Contains("Hidden/Escape/BuildingV33LocalReveal") &&
                    restoredView.growthActiveRenderers == 3 && restoredView.groundActiveRenderers == 3;
                report.revealSupplementStatus = first && second ? "PASS_LOCAL_REVEAL_AND_RESTORE" :
                    "REVIEW_REQUIRED local reveal/restoration did not meet source-shader and 3+3 renderer conditions";
            }
            if (report.status != "RESTORE_FAILED")
                report.status = error == null && poses != null && index >= poses.Length &&
                    views.All(v => v.status == "CAPTURED_REVIEW_REQUIRED") &&
                    report.movementStatus == "PASS_ACTUAL_PLAYER_MOTOR" && report.addedColliders == 0 &&
                    (!IsHudFixturePhase(phase) ||
                        (report.loadoutFingerprint != null &&
                         report.operationTimerAtFreeze == "29:57" &&
                         views.All(v => v.loadoutFingerprint == report.loadoutFingerprint &&
                             v.operationTimeText == "29:57"))) &&
                    (!IsRevealSupplementPhase(phase) ||
                        report.revealSupplementStatus == "PASS_LOCAL_REVEAL_AND_RESTORE") &&
                    (phase != "before_reconstructed" ||
                        (report.rootHiddenDuringCapture && report.rootStateRestored))
                    ? "CAPTURED_STATIC_AND_MOVEMENT_REVIEW_REQUIRED" : "INCOMPLETE_OR_FAILED";
            try
            {
                Directory.CreateDirectory(output ?? Path.Combine(CurrentRecord, "staging/validation_tool/play_" + report.phase));
                File.WriteAllText(Path.Combine(output ?? Path.Combine(CurrentRecord, "staging/validation_tool/play_" + report.phase), "PLAY_REPORT.json"),
                    JsonUtility.ToJson(report, true), Encoding.UTF8);
            }
            catch (Exception write) { Debug.LogError("Clinic mood trial report write failed: " + write); }
            Application.runInBackground = SessionState.GetBool(Key + ".Background", false);
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            initialized = savedState = randomCaptured = false;
            index = stage = frames = walkSteps = 0; lastFrame = -1;
            preview = null; map = null; player = null; earlyInputPlayer = null;
            inventory = null; rifle = null; body = null; camera = null; follow = null; opening = null;
            earlyInputWasLive = false;
            reconstructionRoot = null; reconstructionRootWasActive = false;
            poses = null; report = null; current = null; views.Clear(); enemies.Clear();
            editorMs.Clear(); cpuMs.Clear(); gpuMs.Clear(); draws.Clear(); triangles.Clear();
        }

        static void RequireCleanEdit()
        {
            RequireStableEdit();
            if (SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Clean retained A scene required; this tool never saves it.");
        }
        static void RequireStableEdit()
        {
            if (SessionState.GetBool(Key, false) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Idle Edit mode required; another Play owner may be active.");
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != StageV08Build.Scene)
                throw new InvalidOperationException("Retained A scene required; this tool never saves it.");
        }
        static void RequirePostUiCheckpoint()
        { if (!IsPostUiCheckpoint) throw new InvalidOperationException("SelectPostUiCheckpoint must be called explicitly first."); }
        // The builder calls this inside its single ApplyCandidate02 invocation, immediately
        // before modifying the reviewed root. A previously accepted flag is not sufficient.
        public static string ValidatePostUiBeforeCandidate02()
        {
            RequirePostUiCheckpoint(); RequireCleanEdit();
            string reportPath = Path.Combine(CurrentRecord,
                "staging/validation_tool/play_before_reconstructed/PLAY_REPORT.json");
            string flagPath = Path.Combine(CurrentRecord, "BEFORE_RECONSTRUCTED_ACCEPTED.flag");
            if (!File.Exists(reportPath) || !File.Exists(flagPath))
                throw new FileNotFoundException("Reconstructed before report and manual acceptance flag are required.");
            byte[] reportBytes = File.ReadAllBytes(reportPath);
            string reportHash = Hash(reportBytes);
            string flagHash = FileHash(flagPath);
            AcceptedBeforeFlag acceptance;
            Report before;
            try
            {
                acceptance = JsonUtility.FromJson<AcceptedBeforeFlag>(File.ReadAllText(flagPath));
                int bom = reportBytes.Length >= 3 && reportBytes[0] == 0xef &&
                    reportBytes[1] == 0xbb && reportBytes[2] == 0xbf ? 3 : 0;
                before = JsonUtility.FromJson<Report>(Encoding.UTF8.GetString(reportBytes,
                    bom, reportBytes.Length - bom));
            }
            catch (Exception error) { throw new InvalidOperationException("Invalid reconstructed before report or acceptance flag JSON.", error); }
            if (acceptance == null || !acceptance.manualVisualReview ||
                !string.Equals(acceptance.reportSha256, reportHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Manual review flag is absent or refers to a stale reconstructed before report.");
            string[] expected = ViewIds();
            if (before == null || before.phase != "before_reconstructed" ||
                before.comparisonContext != PostUiSubfolder ||
                before.status != "CAPTURED_STATIC_AND_MOVEMENT_REVIEW_REQUIRED" ||
                before.width != Width || before.height != Height || before.seed != Seed ||
                before.movementStatus != "PASS_ACTUAL_PLAYER_MOTOR" ||
                !before.reconstructedBefore || !before.rootHiddenDuringCapture ||
                !before.rootWasActive || !before.rootStateRestored ||
                before.rootName != AddedRootName || before.addedRenderers != 6 ||
                before.addedColliders != 0 || before.rootAssetSnapshot != RootAssetsBeforeFile ||
                string.IsNullOrEmpty(before.hiddenRootPath) ||
                before.viewCount != expected.Length || before.views == null ||
                before.views.Length != expected.Length)
                throw new InvalidOperationException("Reconstructed before session did not complete the required fixture.");
            for (int i = 0; i < expected.Length; i++)
            {
                View view = before.views[i];
                string gamePath = Path.Combine(CurrentRecord, "before_reconstructed_" + expected[i] + ".png");
                string cameraPath = Path.Combine(CurrentRecord,
                    "staging/validation_tool/play_before_reconstructed/before_reconstructed_camera_" + expected[i] + ".png");
                if (view == null || view.id != expected[i] ||
                    view.status != "CAPTURED_REVIEW_REQUIRED" || view.samples != SampleFrames ||
                    view.rootRenderers != 6 || view.rootActiveRenderers != 0 ||
                    !view.gameExists || !view.cameraExists ||
                    !SamePath(view.gameImage, gamePath) || !SamePath(view.cameraImage, cameraPath) ||
                    !File.Exists(gamePath) || !File.Exists(cameraPath))
                    throw new InvalidOperationException("Reconstructed before view incomplete or altered: " + expected[i]);
            }
            // Recheck every checkpoint source and the owned root in the same editor call
            // that will apply candidate02; no cached acceptance is trusted here.
            ValidateReconstructionSource();
            var mapEdit = UnityEngine.Object.FindFirstObjectByType<StageABlockoutMap>();
            Transform root = mapEdit ? FindAddedRoot(mapEdit.transform) : null;
            if (!root || before.hiddenRootPath != PathOf(root) ||
                reportHash != FileHash(reportPath) || flagHash != FileHash(flagPath))
                throw new InvalidOperationException("Reconstructed before proof or owned root changed during apply gate.");
            string logDirectory = Path.Combine(CurrentRecord, "staging/validation_tool");
            Directory.CreateDirectory(logDirectory);
            string logPath = Path.Combine(logDirectory, "POST_UI_BEFORE_GATE_" +
                DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ") + "_" + Guid.NewGuid().ToString("N") + ".txt");
            using (var stream = new FileStream(logPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.WriteLine("PASS_POST_UI_BEFORE_GATE");
                writer.WriteLine("checkedUtc=" + DateTime.UtcNow.ToString("O"));
                writer.WriteLine("context=" + PostUiSubfolder);
                writer.WriteLine("reportSha256=" + reportHash);
                writer.WriteLine("manualVisualReview=true");
                writer.WriteLine("views=" + expected.Length + " samplesEach=" + SampleFrames +
                    " resolution=" + Width + "x" + Height + " movement=PASS_ACTUAL_PLAYER_MOTOR");
                writer.WriteLine("checkpointSource=scene inventory, four protected sources, 8497 frozen original assets, owned root inventory and 14 dedicated assets rechecked");
            }
            return "PASS_POST_UI_BEFORE_GATE: " + logPath;
        }
        static bool SamePath(string actual, string expected)
        {
            if (string.IsNullOrEmpty(actual)) return false;
            try { return string.Equals(Path.GetFullPath(actual), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }
        static void ValidateReconstructionSource()
        {
            RequirePostUiCheckpoint();
            var mapEdit = UnityEngine.Object.FindFirstObjectByType<StageABlockoutMap>();
            if (!mapEdit || mapEdit.gameObject.scene.path != StageV08Build.Scene)
                throw new InvalidOperationException("Retained A map required for reconstruction source check.");
            Transform root = FindAddedRoot(mapEdit.transform);
            if (!root || !root.gameObject.activeSelf || root.GetComponentsInChildren<Renderer>(true).Length != 6)
                throw new InvalidOperationException("The reviewed candidate01 root must still be active and have six renderers.");
            string[] required = { BaselineFile, ProtectedBeforeFile, AssetsBeforeFile,
                RootBeforeFile, RootAssetsBeforeFile, ReconstructionSourceFile };
            foreach (string relative in required)
                if (!File.Exists(Path.Combine(CurrentRecord, relative)))
                    throw new FileNotFoundException("Post-UI checkpoint source missing: " + relative);
            if (!Inventory(mapEdit.gameObject.scene, mapEdit.transform)
                .SequenceEqual(File.ReadAllLines(Path.Combine(CurrentRecord, BaselineFile))))
                throw new InvalidOperationException("Original scene changed since post-UI checkpoint; reconstruction denied.");
            if (!ProtectedHashes().SequenceEqual(File.ReadAllLines(Path.Combine(CurrentRecord, ProtectedBeforeFile))))
                throw new InvalidOperationException("Post-UI protected sources changed since checkpoint; reconstruction denied.");
            string[] baselineAssets = File.ReadAllLines(Path.Combine(CurrentRecord, AssetsBeforeFile));
            string[] nowAssets = RehashFrozenAssets(baselineAssets, out string assetIssue);
            if (assetIssue != null || !baselineAssets.SequenceEqual(nowAssets))
                throw new InvalidOperationException("Original asset hash differs from post-UI checkpoint: " + (assetIssue ?? "content changed"));
            if (!RootInventory(root).SequenceEqual(File.ReadAllLines(Path.Combine(CurrentRecord, RootBeforeFile))))
                throw new InvalidOperationException("Reviewed candidate01 root hierarchy/bindings changed; reconstruction denied.");
            string[] rootAssets = RootAssetHashes(root, out int dedicatedCount);
            if (dedicatedCount != 14 || !rootAssets.SequenceEqual(File.ReadAllLines(Path.Combine(CurrentRecord, RootAssetsBeforeFile))))
                throw new InvalidOperationException("Reviewed candidate01 root asset hash changed; reconstruction denied.");
        }
        static bool IsOriginalMeshOrMaterial(string path)
        {
            string extension = Path.GetExtension(path);
            return extension.Equals(".mat", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".fbx", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".asset", StringComparison.OrdinalIgnoreCase);
        }
        static bool EquivalentOriginalSceneAcrossUi(string[] oldRows, string[] currentRows,
            out int allowedUiSerializedRows)
        {
            allowedUiSerializedRows = 0;
            if (oldRows == null || currentRows == null || oldRows.Length != currentRows.Length) return false;
            for (int i = 0; i < oldRows.Length; i++)
            {
                if (oldRows[i] == currentRows[i]) continue;
                if (NormalizeV33SerializedRow(oldRows[i]) != NormalizeV33SerializedRow(currentRows[i])) return false;
                allowedUiSerializedRows++;
            }
            return true;
        }
        static string WriteInventoryMismatchDiagnostics(string[] originalRows, string[] currentRows)
        {
            string folder = Path.Combine(CurrentRecord, "staging/validation_tool");
            Directory.CreateDirectory(folder);
            string suffix = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ") + "_" + Guid.NewGuid().ToString("N");
            string currentPath = Path.Combine(folder, "CURRENT_INVENTORY_DIAGNOSTIC_" + suffix + ".tsv");
            string diffPath = Path.Combine(folder, "INVENTORY_EXACT_DIFF_" + suffix + ".txt");
            using (var stream = new FileStream(currentPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
                foreach (string row in currentRows) writer.WriteLine(row);
            var original = originalRows.Skip(1).ToDictionary(InventoryPathKey, StringComparer.Ordinal);
            var current = currentRows.Skip(1).ToDictionary(InventoryPathKey, StringComparer.Ordinal);
            int changed = 0, added = 0, removed = 0, allowedUiOnly = 0;
            using (var stream = new FileStream(diffPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.WriteLine("POST_UI_INVENTORY_MISMATCH_DIAGNOSTIC_ONLY");
                writer.WriteLine("original=" + Path.Combine(Record, BaselineFile));
                writer.WriteLine("current=" + currentPath);
                writer.WriteLine("originalRows=" + originalRows.Length + " currentRows=" + currentRows.Length);
                foreach (string key in original.Keys.Union(current.Keys).OrderBy(k => k, StringComparer.Ordinal))
                {
                    original.TryGetValue(key, out string oldRow);
                    current.TryGetValue(key, out string nowRow);
                    if (oldRow == nowRow) continue;
                    string kind;
                    if (oldRow == null) { kind = "ADDED"; added++; }
                    else if (nowRow == null) { kind = "REMOVED"; removed++; }
                    else if (NormalizeV33SerializedRow(oldRow) == NormalizeV33SerializedRow(nowRow))
                    { kind = "V33_SERIALIZED_ONLY"; allowedUiOnly++; }
                    else { kind = "CHANGED_OUTSIDE_ALLOWED_UI"; changed++; }
                    writer.WriteLine("--- " + kind + " " + key);
                    writer.WriteLine("OLD " + (oldRow ?? "<none>"));
                    writer.WriteLine("NEW " + (nowRow ?? "<none>"));
                }
                writer.WriteLine("SUMMARY added=" + added + " removed=" + removed +
                    " changedOutsideAllowedUi=" + changed + " v33SerializedOnly=" + allowedUiOnly);
                writer.WriteLine("This file diagnoses the failed checkpoint. It does not approve a new baseline or relax preservation checks.");
            }
            return diffPath;
        }
        static string InventoryPathKey(string row)
        {
            int tab = row.IndexOf('\t');
            return tab < 0 ? row : row.Substring(0, tab);
        }
        static string NormalizeV33SerializedRow(string row)
        {
            int first = row.IndexOf('\t');
            int second = first < 0 ? -1 : row.IndexOf('\t', first + 1);
            if (second < 0) return row;
            string path = row.Substring(0, first);
            string detail = row.Substring(second + 1);
            const string marker = " || Escape.Stage.Blockout.StageABuildingV33Reveal json=";
            int start = detail.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return path + "\t" + detail;
            int jsonStart = start + marker.Length;
            int next = detail.IndexOf(" || ", jsonStart, StringComparison.Ordinal);
            string suffix = next < 0 ? "" : detail.Substring(next);
            return path + "\t" + detail.Substring(0, jsonStart) + "__UI_FIELDS__" + suffix;
        }
        static string[] RootInventory(Transform root)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .Select(t => PathOf(t) + "\t" + Hash(Encoding.UTF8.GetBytes(Describe(t))) + "\t" + Describe(t))
                .OrderBy(row => row, StringComparer.Ordinal).ToArray();
        }
        static string[] RootAssetHashes(Transform root, out int dedicatedCount)
        {
            var paths = new SortedSet<string>(StringComparer.Ordinal);
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials) Add(material);
                if (renderer is SkinnedMeshRenderer skin) Add(skin.sharedMesh);
            }
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true)) Add(filter.sharedMesh);
            dedicatedCount = paths.Count(p => p.StartsWith(AddedAssetPrefix, StringComparison.Ordinal));
            foreach (string dependency in AssetDatabase.GetDependencies(paths.ToArray(), true))
                if (dependency.StartsWith("Assets/", StringComparison.Ordinal)) paths.Add(dependency);
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return paths.Select(path => {
                string full = Path.Combine(projectRoot, path);
                return path + "\t" + (File.Exists(full) ? FileHash(full) : "MISSING");
            }).ToArray();
            void Add(UnityEngine.Object obj)
            {
                if (!obj) return;
                string path = AssetDatabase.GetAssetPath(obj);
                if (path.StartsWith("Assets/", StringComparison.Ordinal)) paths.Add(path);
            }
        }
        static void RefuseExisting(string path)
        { if (File.Exists(path)) throw new IOException("Prior evidence must not be overwritten: " + path); }
        static string SafeCandidateTag(string candidateTag)
        {
            if (string.IsNullOrWhiteSpace(candidateTag) ||
                !Regex.IsMatch(candidateTag, "^[a-z][a-z0-9_-]{0,63}$", RegexOptions.CultureInvariant))
                throw new ArgumentException("Candidate tag must be lowercase letters/digits/_/- and start with a letter, max 64 characters.");
            return candidateTag;
        }
        static Transform FindAddedRoot(Transform root)
        {
            var structure = root.GetComponent<StageABlockoutStructureV08>();
            if (!structure) structure = root.GetComponentInChildren<StageABlockoutStructureV08>(true);
            var clinic = structure != null ? structure.FindAnchor("CLINIC") : null;
            return clinic != null && clinic.VisualRoot ? clinic.VisualRoot.Find(AddedRootName) : null;
        }

        static string[] Inventory(Scene scene, Transform mapRoot)
        {
            var lines = new List<string> { "path\tsha256\tdetail" };
            Transform added = FindAddedRoot(mapRoot);
            foreach (Transform t in scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Transform>(true)))
            {
                if (t == null || (added && (t == added || t.IsChildOf(added)))) continue;
                string detail = Describe(t);
                lines.Add(PathOf(t) + "\t" + Hash(Encoding.UTF8.GetBytes(detail)) + "\t" + detail);
            }
            lines.Sort(1, lines.Count - 1, StringComparer.Ordinal);
            return lines.ToArray();
        }
        static string Describe(Transform t)
        {
            var sb = new StringBuilder();
            sb.Append("active=").Append(t.gameObject.activeSelf).Append(" layer=").Append(t.gameObject.layer)
                .Append(" tag=").Append(t.gameObject.tag)
                .Append(" local=").Append(t.localPosition.ToString("F6"))
                .Append(t.localRotation.ToString("F6")).Append(t.localScale.ToString("F6"));
            foreach (Component c in t.GetComponents<Component>())
            {
                if (!c) { sb.Append(" missing-script"); continue; }
                sb.Append(" || ").Append(c.GetType().FullName);
                if (c is Renderer r)
                {
                    sb.Append(" enabled=").Append(r.enabled).Append(" shadow=").Append(r.shadowCastingMode)
                        .Append(" receive=").Append(r.receiveShadows)
                        .Append(" mats=").Append(string.Join(",", r.sharedMaterials.Select(AssetIdentity)));
                }
                else if (c is MeshFilter mf) sb.Append(" mesh=").Append(AssetIdentity(mf.sharedMesh));
                else if (c is SkinnedMeshRenderer smr) sb.Append(" mesh=").Append(AssetIdentity(smr.sharedMesh));
                else if (c is Collider col) sb.Append(" enabled=").Append(col.enabled)
                    .Append(" trigger=").Append(col.isTrigger).Append(" json=").Append(EditorJsonUtility.ToJson(c));
                else if (!(c is Transform)) sb.Append(" json=").Append(EditorJsonUtility.ToJson(c));
            }
            return sb.ToString().Replace('\t', ' ').Replace('\n', ' ');
        }
        static string PathOf(Transform t)
        {
            var parts = new List<string>();
            for (Transform p = t; p; p = p.parent)
            {
                int twin = 0;
                if (p.parent) for (int i = 0; i < p.GetSiblingIndex(); i++)
                    if (p.parent.GetChild(i).name == p.name) twin++;
                parts.Add(p.name + "#" + twin);
            }
            parts.Reverse(); return string.Join("/", parts);
        }
        static string AssetIdentity(UnityEngine.Object obj)
        {
            if (!obj) return "null";
            string path = AssetDatabase.GetAssetPath(obj);
            string guid = string.IsNullOrEmpty(path) ? "runtime-or-embedded" : AssetDatabase.AssetPathToGUID(path);
            return guid + ":" + path + ":" + obj.name;
        }
        static string[] ProtectedHashes()
        {
            var lines = new List<string>();
            foreach (string path in ProtectedFiles)
            {
                string full = Path.Combine(Directory.GetParent(Application.dataPath).FullName, path);
                lines.Add(path + "\t" + (File.Exists(full) ? Hash(File.ReadAllBytes(full)) : "MISSING"));
            }
            return lines.ToArray();
        }
        static string[] ReferencedAssetHashes(Scene scene, Transform mapRoot)
        {
            Transform added = FindAddedRoot(mapRoot);
            var paths = new SortedSet<string>(StringComparer.Ordinal);
            foreach (Transform t in scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Transform>(true)))
            {
                if (added && (t == added || t.IsChildOf(added))) continue;
                foreach (Renderer r in t.GetComponents<Renderer>())
                {
                    foreach (Material m in r.sharedMaterials) AddPath(m);
                    if (r is SkinnedMeshRenderer smr) AddPath(smr.sharedMesh);
                }
                foreach (MeshFilter mf in t.GetComponents<MeshFilter>()) AddPath(mf.sharedMesh);
            }
            foreach (string dependency in AssetDatabase.GetDependencies(paths.ToArray(), true))
                if (dependency.StartsWith("Assets/", StringComparison.Ordinal)) paths.Add(dependency);
            return paths.Select(path => {
                string full = Path.Combine(Directory.GetParent(Application.dataPath).FullName, path);
                return path + "\t" + (File.Exists(full) ? FileHash(full) : "MISSING");
            }).ToArray();
            void AddPath(UnityEngine.Object asset)
            {
                if (!asset) return;
                string path = AssetDatabase.GetAssetPath(asset);
                if (path.StartsWith("Assets/", StringComparison.Ordinal)) paths.Add(path);
            }
        }
        static string[] RehashFrozenAssets(string[] baseline, out string issue)
        {
            issue = null;
            if (baseline == null || baseline.Length != ExpectedReferencedAssetCount)
            {
                issue = "before asset count differs from frozen " + ExpectedReferencedAssetCount;
                return Array.Empty<string>();
            }
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string projectPrefix = Path.GetFullPath(projectRoot) + Path.DirectorySeparatorChar;
            var lines = new string[baseline.Length];
            string previousPath = "";
            for (int i = 0; i < baseline.Length; i++)
            {
                string line = baseline[i];
                int tab = line.IndexOf('\t');
                if (tab <= 0 || tab != line.LastIndexOf('\t'))
                { issue = "malformed before asset row " + i; return Array.Empty<string>(); }
                string path = line.Substring(0, tab);
                string oldHash = line.Substring(tab + 1);
                if (!path.StartsWith("Assets/", StringComparison.Ordinal) ||
                    path.Contains("..") || string.CompareOrdinal(previousPath, path) >= 0 ||
                    !Regex.IsMatch(oldHash, "^[0-9a-f]{64}$", RegexOptions.CultureInvariant))
                { issue = "invalid, duplicate, or unsorted before asset path/hash at row " + i;
                  return Array.Empty<string>(); }
                string full = Path.GetFullPath(Path.Combine(projectRoot, path));
                if (!full.StartsWith(projectPrefix, StringComparison.OrdinalIgnoreCase))
                { issue = "before asset path escaped Unity project at row " + i; return Array.Empty<string>(); }
                previousPath = path;
                if (!File.Exists(full))
                { if (issue == null) issue = "missing original asset: " + path;
                  lines[i] = path + "\tMISSING"; }
                else lines[i] = path + "\t" + FileHash(full);
            }
            return lines;
        }
        static string FileHash(string path)
        { using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
        static string Hash(byte[] bytes)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        static string EnvironmentFingerprint()
        {
            var sb = new StringBuilder();
            sb.Append("ambient=").Append(RenderSettings.ambientLight.ToString())
                .Append(" fog=").Append(RenderSettings.fog).Append(':')
                .Append(RenderSettings.fogColor.ToString()).Append(':')
                .Append(RenderSettings.fogDensity.ToString("F6"))
                .Append(" skybox=").Append(RenderSettings.skybox ? RenderSettings.skybox.name : "null");
            foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .Where(l => l && l.type == LightType.Directional).OrderBy(l => PathOf(l.transform)))
                sb.Append(" | ").Append(PathOf(light.transform)).Append(':').Append(light.enabled)
                    .Append(':').Append(light.transform.rotation.ToString("F5"))
                    .Append(':').Append(light.color.ToString()).Append(':').Append(light.intensity.ToString("F5"));
            return sb.ToString();
        }
        static int TriangleCount(Renderer renderer)
        {
            Mesh mesh = null;
            if (renderer is SkinnedMeshRenderer smr) mesh = smr.sharedMesh;
            else { var mf = renderer.GetComponent<MeshFilter>(); if (mf) mesh = mf.sharedMesh; }
            if (!mesh) return 0;
            int count = 0;
            for (int i = 0; i < mesh.subMeshCount; i++) count += (int)mesh.GetIndexCount(i) / 3;
            return count;
        }
        static float Percentile(List<float> values, float fraction)
        {
            if (values.Count == 0) return -1;
            float[] sorted = values.OrderBy(x => x).ToArray();
            return sorted[Mathf.Clamp(Mathf.FloorToInt((sorted.Length - 1) * fraction), 0, sorted.Length - 1)];
        }
    }
}
#endif
