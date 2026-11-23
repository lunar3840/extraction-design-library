#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Escape.Stage.Blockout;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Escape.EditorTools.ClinicMoodTrial
{
    // Temporary owner-scoped bridge. No action without this trial's explicit request.
    [InitializeOnLoad]
    public static class ClinicMoodTrialBridge
    {
        const string RequestPath = "Library/ClinicMoodTrial20261030.request";
        const string Record = "C:/Users/ehoji/OneDrive/문서/ChatGPT/익스트랙션/outputs/change_records/2026-10-30/a-map-clinic-reclaimed-mood-trial";
        [Serializable] sealed class Command { public string command, id, tag; }
        static ClinicMoodTrialBridge() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(RequestPath)) return;
            string text = File.ReadAllText(RequestPath);
            File.Delete(RequestPath);
            Command command = null;
            try
            {
                command = JsonUtility.FromJson<Command>(text);
                if (command == null || string.IsNullOrEmpty(command.id) || command.id.Any(c => !char.IsLetterOrDigit(c) && c != '_' && c != '-'))
                    throw new InvalidOperationException("Explicit command id required.");
                if (File.Exists(Path.Combine(Record, "COMMAND_" + command.id + ".txt")) ||
                    File.Exists(Path.Combine(Record, "COMMAND_" + command.id + "_ERROR.txt")))
                    throw new InvalidOperationException("Refusing to reuse command id: " + command.id);
                string result;
                switch (command.command)
                {
                    case "status": result = Status(); break;
                    case "snapshot": result = StageAClinicMoodTrialValidation.SnapshotBefore(); break;
                    case "post_ui_snapshot":
                        RequireEdit();
                        result = StageAClinicMoodTrialValidation.SelectPostUiCheckpoint() + "\n" + StageAClinicMoodTrialValidation.SnapshotPostUiCheckpoint(); break;
                    case "post_ui_before":
                        RequireEdit();
                        StageAClinicMoodTrialValidation.SelectPostUiCheckpoint();
                        result = StageAClinicMoodTrialValidation.StartPostUiReconstructedBefore(); break;
                    case "post_ui_context": result = StageAClinicMoodTrialValidation.SelectPostUiCheckpoint(); break;
                    case "before": result = StageAClinicMoodTrialValidation.StartPlay("before"); break;
                    case "after": result = string.IsNullOrEmpty(command.tag) ? StageAClinicMoodTrialValidation.StartPlay("after") : StageAClinicMoodTrialValidation.StartPlayAfter(command.tag); break;
                    case "reveal_supplement": result = StageAClinicMoodTrialValidation.StartRevealSupplement(); break;
                    case "compare": result = string.IsNullOrEmpty(command.tag) ? StageAClinicMoodTrialValidation.CompareAfter() : StageAClinicMoodTrialValidation.CompareAfterTagged(command.tag); break;
                    case "pairs": result = string.IsNullOrEmpty(command.tag) ? StageAClinicMoodTrialValidation.ComparePlayPairs() : StageAClinicMoodTrialValidation.ComparePlayPairsAfter(command.tag); break;
                    case "apply": result = Apply(); break;
                    case "apply_candidate02":
                        RequireEdit();
                        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Candidate02 requires a clean post-UI checkpoint.");
                        if (!File.Exists(Path.Combine(Record, "rounds/post_ui_checkpoint/BEFORE_RECONSTRUCTED_ACCEPTED.flag")))
                            throw new InvalidOperationException("Main must review the reconstructed post-UI before first.");
                        result = InvokeBuilder("ApplyCandidate02"); break;
                    case "probe": RequireEdit(); result = InvokeBuilder("Probe"); break;
                    case "diagnose_owned_roots": RequireEdit(); result = InvokeBuilder("DiagnoseOwnedRoots"); break;
                    case "repair_own_duplicate": result = RepairOwnDuplicate(); break;
                    case "audit": RequireEdit(); result = InvokeBuilder("Audit"); break;
                    case "rollback": RequireEdit(); result = InvokeBuilder("Rollback"); break;
                    case "cancel": result = StageAClinicMoodTrialValidation.Cancel(); break;
                    case "save": result = Save(command.id); break;
                    case "reload":
                        RequireEdit();
                        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Refusing to reload dirty scene.");
                        EditorSceneManager.OpenScene(StageV08Build.Scene, OpenSceneMode.Single);
                        result = Status(); break;
                    default: throw new InvalidOperationException("Unknown owner-scoped command.");
                }
                Directory.CreateDirectory(Record);
                using (var stream = new FileStream(Path.Combine(Record, "COMMAND_" + command.id + ".txt"), FileMode.CreateNew))
                using (var writer = new StreamWriter(stream))
                    writer.Write("START " + DateTime.UtcNow.ToString("O") + "\ncommand=" + command.command + "\n" + result + "\nCOMPLETE\n");
            }
            catch (Exception exception)
            {
                Directory.CreateDirectory(Record);
                string errorId = command != null && !string.IsNullOrEmpty(command.id) && command.id.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-') ? command.id : "invalid";
                string errorPath = Path.Combine(Record, "COMMAND_" + errorId + "_ERROR.txt");
                if (File.Exists(errorPath)) errorPath += "." + DateTime.UtcNow.Ticks + ".txt";
                using (var stream = new FileStream(errorPath, FileMode.CreateNew))
                using (var writer = new StreamWriter(stream)) writer.Write(text + "\n" + exception);
                Debug.LogException(exception);
            }
        }
        static string Apply()
        {
            RequireEdit();
            if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("First apply requires a clean scene.");
            if (!File.Exists(Path.Combine(Record, "BEFORE_ACCEPTED.flag"))) throw new InvalidOperationException("Main must review true before evidence first.");
            return InvokeBuilder("Apply");
        }
        static string Save(string commandId)
        {
            RequireEdit();
            var map = UnityEngine.Object.FindFirstObjectByType<StageABlockoutMap>();
            if (!map || map.gameObject.scene.path != StageV08Build.Scene) throw new InvalidOperationException("Retained A scene required.");
            var structure = map.GetComponent<StageABlockoutStructureV08>();
            var clinic = structure ? structure.FindAnchor("CLINIC") : null;
            var root = clinic && clinic.VisualRoot ? clinic.VisualRoot.Find("__A34_ClinicMoodTrial") : null;
            if (!root || OwnedRoots(map).Length != 1 || root.GetComponentsInChildren<Collider>(true).Length != 0)
                throw new InvalidOperationException("Reviewed direct CLINIC trial root / zero collider contract failed.");
            string audit = InvokeBuilder("Audit");
            if (!audit.Contains("RESULT PASS")) throw new InvalidOperationException("Builder audit failed before save: " + audit);
            string preservation = StageAClinicMoodTrialValidation.CompareWorkingCopy("save_" + commandId);
            if (!preservation.StartsWith("PASS_STATIC_ONLY", StringComparison.Ordinal))
                throw new InvalidOperationException("Retained scene/assets changed before save: " + preservation);
            bool addedOverride = PrefabUtility.IsAddedGameObjectOverride(root.gameObject);
            PrefabUtility.SaveAsPrefabAsset(map.gameObject, StageV08Build.Prefab, out bool ok);
            if (!ok) throw new InvalidOperationException("A prefab save failed.");
            // The prefab now owns this exact authored root. Leaving its scene-added
            // override would instantiate it twice on reload. Revert only that root.
            if (addedOverride && root && PrefabUtility.IsAddedGameObjectOverride(root.gameObject))
                PrefabUtility.RevertAddedGameObject(root.gameObject, InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
            if (!EditorSceneManager.SaveScene(map.gameObject.scene)) throw new InvalidOperationException("A scene save failed.");
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(StageV08Build.Scene, OpenSceneMode.Single);
            map = UnityEngine.Object.FindFirstObjectByType<StageABlockoutMap>();
            AssertOneOwnedRoot(map);
            string reloadAudit = InvokeBuilder("Audit");
            if (!reloadAudit.Contains("RESULT PASS")) throw new InvalidOperationException("Reload audit failed: " + reloadAudit);
            string reloaded = StageAClinicMoodTrialValidation.CompareWorkingCopy("reloaded_" + commandId);
            if (!reloaded.StartsWith("PASS_STATIC_ONLY", StringComparison.Ordinal))
                throw new InvalidOperationException("Reload preservation failed: " + reloaded);
            return Status() + "\nownedDirectRoots=1\nownedRenderers=6\n" + reloadAudit + reloaded;
        }
        static Transform[] OwnedRoots(StageABlockoutMap map)
        {
            var structure = map ? map.GetComponent<StageABlockoutStructureV08>() : null;
            var clinic = structure ? structure.FindAnchor("CLINIC") : null;
            if (!clinic || !clinic.VisualRoot) throw new InvalidOperationException("CLINIC visual root missing.");
            return clinic.VisualRoot.Cast<Transform>().Where(t => t.name == "__A34_ClinicMoodTrial").ToArray();
        }
        static void AssertOneOwnedRoot(StageABlockoutMap map)
        {
            var roots = OwnedRoots(map);
            if (roots.Length != 1 || roots[0].GetComponentsInChildren<Renderer>(true).Length != 6 ||
                roots[0].GetComponentsInChildren<Collider>(true).Length != 0 ||
                roots[0].GetComponentsInChildren<Light>(true).Length != 0 ||
                !PrefabUtility.GetCorrespondingObjectFromSource(roots[0].gameObject) ||
                PrefabUtility.IsAddedGameObjectOverride(roots[0].gameObject))
                throw new InvalidOperationException("Reload must contain exactly one prefab-owned A34 root, 6 renderers, collider/light 0.");
        }
        static string RepairOwnDuplicate()
        {
            RequireEdit();
            if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Duplicate repair needs clean Edit.");
            var map = UnityEngine.Object.FindFirstObjectByType<StageABlockoutMap>();
            if (!map || map.gameObject.scene.path != StageV08Build.Scene) throw new InvalidOperationException("Retained A required.");
            var roots = OwnedRoots(map);
            if (roots.Length != 2) throw new InvalidOperationException("Expected exactly the diagnosed pair.");
            var added = roots.Where(t => PrefabUtility.IsAddedGameObjectOverride(t.gameObject)).ToArray();
            var source = roots.Where(t => PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject) &&
                !PrefabUtility.IsAddedGameObjectOverride(t.gameObject)).ToArray();
            if (added.Length != 1 || source.Length != 1 ||
                roots.Any(t => t.GetComponentsInChildren<Renderer>(true).Length != 6 ||
                    t.GetComponentsInChildren<Collider>(true).Length != 0 || t.GetComponentsInChildren<Light>(true).Length != 0))
                throw new InvalidOperationException("Pair is not the diagnosed source plus scene-added owned root.");
            if (OwnedGeometrySignature(added[0]) != OwnedGeometrySignature(source[0]))
                throw new InvalidOperationException("Duplicate authored geometry/materials/transforms differ; manual diagnosis required.");
            var addedIvy = added[0].GetComponentInChildren<Escape.SurfaceIvy.SurfaceIvyInstance>(true);
            var sourceIvy = source[0].GetComponentInChildren<Escape.SurfaceIvy.SurfaceIvyInstance>(true);
            if (!addedIvy || !sourceIvy || addedIvy.sourceTarget != sourceIvy.sourceTarget ||
                !addedIvy.sourceRenderers.SequenceEqual(sourceIvy.sourceRenderers))
                throw new InvalidOperationException("Duplicate source visibility bindings differ.");
            string prefabBefore = FileSha(StageV08Build.Prefab);
            PrefabUtility.RevertAddedGameObject(added[0].gameObject, InteractionMode.AutomatedAction);
            AssertOneOwnedRoot(map);
            VerifyOriginalInventoryExact(map);
            EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
            if (!EditorSceneManager.SaveScene(map.gameObject.scene)) throw new InvalidOperationException("Repair scene save failed.");
            EditorSceneManager.OpenScene(StageV08Build.Scene, OpenSceneMode.Single);
            map = UnityEngine.Object.FindFirstObjectByType<StageABlockoutMap>();
            AssertOneOwnedRoot(map);
            VerifyOriginalInventoryExact(map);
            if (prefabBefore != FileSha(StageV08Build.Prefab)) throw new InvalidOperationException("Repair changed prefab unexpectedly.");
            return "REPAIRED_ONLY_OWN_SCENE_ADDED_OVERRIDE\noriginalInventoryExact=true\nprefabUnchanged=true\nownedDirectRoots=1\nownedRenderers=6\n" + Status();
        }
        static void VerifyOriginalInventoryExact(StageABlockoutMap map)
        {
            var method = typeof(StageAClinicMoodTrialValidation).GetMethod("Inventory", BindingFlags.Static | BindingFlags.NonPublic);
            var now = (string[])method.Invoke(null, new object[] { map.gameObject.scene, map.transform });
            var original = File.ReadAllLines(Path.Combine(Record, "staging/validation_tool/BEFORE_INVENTORY.tsv"));
            if (!original.SequenceEqual(now)) throw new InvalidOperationException("Original scene changed beyond the owned duplicate.");
        }
        static string FileSha(string path)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
        }
        static string OwnedGeometrySignature(Transform root)
        {
            var describe = typeof(StageAClinicMoodTrialValidation).GetMethod("Describe", BindingFlags.Static | BindingFlags.NonPublic);
            return string.Join("\n", root.GetComponentsInChildren<Transform>(true).Select(t => {
                string path = t == root ? "." : AnimationUtility.CalculateTransformPath(t, root);
                return path + "|" + (string)describe.Invoke(null, new object[] { t });
            }).OrderBy(s => s, StringComparer.Ordinal));
        }
        static string InvokeBuilder(string name)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Escape.EditorTools.StageAClinicMoodTrialBuilder", false)).Single(t => t != null);
            var method = type.GetMethod(name, BindingFlags.Static | BindingFlags.Public, null, Type.EmptyTypes, null);
            if (method == null) throw new InvalidOperationException("Reviewed builder method absent: " + name);
            return Convert.ToString(method.Invoke(null, null));
        }
        static void RequireEdit()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Stable Edit mode required.");
        }
        static string Status()
        {
            return "playing=" + EditorApplication.isPlaying + "\ncompiling=" + EditorApplication.isCompiling +
                "\nscene=" + SceneManager.GetActiveScene().path + "\ndirty=" + SceneManager.GetActiveScene().isDirty +
                "\nenemies=" + UnityEngine.Object.FindObjectsByType<Escape.Enemy.EnemyController>(FindObjectsSortMode.None).Length;
        }
    }
}
#endif
