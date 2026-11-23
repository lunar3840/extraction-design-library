#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Unity.Collections;
using Escape.EditorTools.Ruin;
using Escape.EditorTools.RuinStudy;
using Escape.EditorTools.Ivy;
using Escape.Stage.Blockout;
using Escape.SurfaceIvy;
using Escape.SurfaceWeathering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Escape.EditorTools
{
    // Editor-only Stage A integration. The source scene/prefab is saved by the caller.
    // No source asset, collider, functional component or Transform is rewritten here.
    public static class StageARuinRolloutBuilder
    {
        public const string DefaultAssetRoot = "Assets/Escape/Art/SurfaceRuin/StageARollout";
        const string WeatherShader = "Escape/Environment/Weathered Lit";
        const string D09Shader = "Escape/BuildingD09/WeatheredLit";
        const string UrpShader = "Universal Render Pipeline/Lit";
        const string OwnedRoot = "__A33_Ruin_Ivy_Generated";

        [Serializable] public sealed class PolicyFile
        {
            public int schema;
            public PolicyEntry[] entries = Array.Empty<PolicyEntry>();
        }

        [Serializable] public sealed class PolicyEntry
        {
            public string key, path, assetClass, surfaceClass, weatheringMode;
            public string ivyMode, ivyGroup, ivyTargetPath, damageMode, reuseKey;
            public string[] reasons = Array.Empty<string>();
            public float maxDamage, minCutHeight = 2.1f;
            public float floorWorldY, minCutWorldY;
            public int seed, ivyLeafBudget;
            public WorldExclusion[] ivyExclusionsWorld = Array.Empty<WorldExclusion>();
        }

        [Serializable] public sealed class WorldExclusion
        {
            public Vector3 center, size;
            public string source;
        }

        public sealed class Row
        {
            public string key;
            public MeshRenderer renderer;
            public MeshFilter filter;
            public PolicyEntry policy;
        }

        public sealed class RolloutPlan
        {
            public StageABlockoutMap map;
            public readonly List<Row> rows = new List<Row>();
            public readonly List<string> errors = new List<string>();
            public int policyRows, protectedRows, weatherRows, damageCandidates, ivyGroups;
        }

        [Serializable] public sealed class RowResult
        {
            public string key, status, weather, damage, ivy, detail;
        }

        [Serializable] public sealed class RolloutResult
        {
            public int total, protectedRows, treatedRows, unhandledRows, failedRows, weatherSlots;
            public int damageMeshes, ivyGroups, ivyLeaves, generatedMeshes, generatedMaterials;
            public RowResult[] rows;
            public string summary;
        }

        [Serializable] public sealed class IvyRefineRunReport
        {
            public string startedUtc, finishedUtc, sourceResultPath;
            public int plannedIvyGroups, attemptedIvyGroups, succeededIvyGroups, failedIvyGroups;
            public int ivyGroupsAfter, ivyLeavesAfter, weatherSlotsAdded;
            public int generatedMeshesThisRun, generatedMaterialsThisRun;
            public string[] failures;
        }

        public static RolloutPlan Plan(StageABlockoutMap map, string policyJsonPath)
        {
            if (!map) throw new ArgumentNullException(nameof(map));
            if (string.IsNullOrEmpty(policyJsonPath) || !File.Exists(policyJsonPath))
                throw new FileNotFoundException("A33 requires the reviewed full-row policy", policyJsonPath);
            var policy = JsonUtility.FromJson<PolicyFile>(File.ReadAllText(policyJsonPath));
            if (policy == null || policy.schema != 1 || policy.entries == null)
                throw new InvalidOperationException("Unsupported A33 policy schema");
            var plan = new RolloutPlan { map = map, policyRows = policy.entries.Length };
            var choices = new Dictionary<string, PolicyEntry>(StringComparer.Ordinal);
            foreach (var entry in policy.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.key) || !choices.TryAdd(entry.key, entry))
                    plan.errors.Add("Missing or duplicate policy key: " + (entry == null ? "<null>" : entry.key));
            }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var renderer in map.GetComponentsInChildren<Renderer>(true))
            {
                if (OwnedAncestor(renderer.transform, map.transform)) continue;
                string key = Key(renderer.transform, map.transform);
                if (!seen.Add(key)) plan.errors.Add("Duplicate renderer path: " + key);
                if (!choices.TryGetValue(key, out var entry))
                {
                    plan.errors.Add("Renderer missing from full-row policy: " + key);
                    continue;
                }
                var row = new Row { key = key, renderer = renderer as MeshRenderer,
                    filter = renderer.GetComponent<MeshFilter>(), policy = entry };
                plan.rows.Add(row);
                if (Protected(entry)) plan.protectedRows++;
                else
                {
                    if (Mode(entry.weatheringMode, "Tool") || Mode(entry.weatheringMode, "CustomVariantOrOverlay")) plan.weatherRows++;
                    if (Mode(entry.damageMode, "Apply") || Mode(entry.damageMode, "SafeChip")) plan.damageCandidates++;
                }
            }
            foreach (string key in choices.Keys)
                if (!seen.Contains(key)) plan.errors.Add("Policy key missing from current map: " + key);
            plan.ivyGroups = plan.rows.Where(r => Mode(r.policy.ivyMode, "Apply"))
                .Select(r => r.policy.ivyGroup ?? r.key).Distinct(StringComparer.Ordinal).Count();
            if (plan.rows.Count != policy.entries.Length)
                plan.errors.Add("Policy and map renderer counts differ: " + policy.entries.Length + " / " + plan.rows.Count);
            return plan;
        }

        public static RolloutResult RefineIvy(StageABlockoutMap map, RolloutPlan plan,
            string resultJsonPath, string generatedAssetRoot = DefaultAssetRoot,
            string runReportJsonPath = null)
        {
            if (!map || plan == null || plan.map != map)
                throw new ArgumentException("Plan/map mismatch");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Edit mode required");
            if (plan.errors.Count != 0)
                throw new InvalidOperationException("Policy preflight failed: " +
                    string.Join(" | ", plan.errors.Take(30)));
            if (string.IsNullOrEmpty(resultJsonPath) || !File.Exists(resultJsonPath))
                throw new FileNotFoundException("Existing rollout result required", resultJsonPath);
        
            string root = AssetRoot(generatedAssetRoot);
            var baseline = AssetDatabase.LoadAssetAtPath<StageARuinRolloutBaseline>(root + "/A33Baseline.asset");
            if (!baseline || baseline.rows == null)
                throw new InvalidOperationException("Existing A33 source baseline required");
            var baselineByKey = baseline.rows.ToDictionary(r => r.key, StringComparer.Ordinal);
            var result = JsonUtility.FromJson<RolloutResult>(File.ReadAllText(resultJsonPath));
            if (result == null || result.rows == null || result.total != plan.rows.Count ||
                result.rows.Length != plan.rows.Count)
                throw new InvalidOperationException("Prior result/map row count mismatch");
            var resultByKey = result.rows.ToDictionary(r => r.key, StringComparer.Ordinal);
            if (resultByKey.Count != plan.rows.Count ||
                plan.rows.Any(r => !resultByKey.ContainsKey(r.key)))
                throw new InvalidOperationException("Prior result/map keys mismatch");
        
            var representatives = plan.rows.Where(r => Mode(r.policy.ivyMode, "Apply"))
                .OrderBy(r => r.key, StringComparer.Ordinal).ToList();
            int distinctGroups = representatives.Select(r => string.IsNullOrEmpty(r.policy.ivyGroup) ?
                r.key : r.policy.ivyGroup).Distinct(StringComparer.Ordinal).Count();
            if (representatives.Count != 226 || distinctGroups != 226 || plan.ivyGroups != 226)
                throw new InvalidOperationException("Expected exactly 226 unique ivy representatives");
            if (representatives.Any(r => !baselineByKey.ContainsKey(r.key)))
                throw new InvalidOperationException("An ivy representative lacks source baseline");
        
            var salvageCar = representatives.Single(r =>
                r.key.Contains("/SW_W_SalvageCar[6]/") && r.key.EndsWith("/Model[0]", StringComparison.Ordinal));
            var run = new IvyRefineRunReport { startedUtc = DateTime.UtcNow.ToString("O"),
                sourceResultPath = resultJsonPath, plannedIvyGroups = representatives.Count };
            var failures = new List<string>();
            var operation = new RolloutResult(); // counters for this invocation only
            var materialCache = new Dictionary<string, Material>(StringComparer.Ordinal);
            Shader weatherShader = Shader.Find(WeatherShader);
            if (!weatherShader) throw new InvalidOperationException("Missing " + WeatherShader);
        
            string backupPath = resultJsonPath + ".pre-ivy-refine.json";
            if (!File.Exists(backupPath)) File.Copy(resultJsonPath, backupPath);
        
            foreach (Row row in representatives)
            {
                var item = resultByKey[row.key];
                var errors = new List<string>();
                run.attemptedIvyGroups++;
                if (!Eligible(row, out string issue))
                {
                    errors.Add("ivy ineligible: " + issue);
                    item.ivy = "refine-failed: " + issue;
                }
                else
                {
                    if (row == salvageCar && item.weather.StartsWith("tool-aged-slots=0",StringComparison.Ordinal))
                    {
                        Material[] priorMaterials = (Material[])row.renderer.sharedMaterials.Clone();
                        var priorBinding = row.renderer.GetComponent<AMapRuinWeatheringBinding>();
                        int weatherBefore = operation.weatherSlots;
                        try
                        {
                            if (baselineByKey[row.key].sourcePropertyBlockOccupied)
                                throw new InvalidOperationException("Source property block protects weathering");
                            item.weather = ApplyWeather(row, root, weatherShader,
                                materialCache, row.policy.seed != 0 ? row.policy.seed : StableSeed(row.key), operation);
                            if (operation.weatherSlots != weatherBefore + 1)
                                throw new InvalidOperationException("SalvageCar weather slot delta was not one");
                        }
                        catch (Exception exception)
                        {
                            row.renderer.sharedMaterials = priorMaterials;
                            operation.weatherSlots = weatherBefore;
                            var addedBinding = row.renderer.GetComponent<AMapRuinWeatheringBinding>();
                            if (!priorBinding && addedBinding) Object.DestroyImmediate(addedBinding);
                            item.weather = "refine-failed: " + exception.Message;
                            errors.Add("weather: " + exception.Message);
                        }
                    }
        
                    Transform target = string.IsNullOrEmpty(row.policy.ivyTargetPath) ?
                        row.renderer.transform : map.transform.Find(row.policy.ivyTargetPath);
                    if (!target)
                    {
                        item.ivy = "refine-failed: missing target";
                        errors.Add("ivy target missing: " + row.policy.ivyTargetPath);
                    }
                    else
                    {
                        Transform own = row.renderer.transform.Find(OwnedRoot);
                        if (target == row.renderer.transform && !own)
                        {
                            var created = new GameObject(OwnedRoot);
                            created.transform.SetParent(row.renderer.transform, false);
                            own = created.transform;
                        }
                        if (!own) own = row.renderer.transform; // special target branch in ApplyIvy replaces it
                        Mesh sourceMesh = row.filter.sharedMesh;
                        Material[] sourceMaterials = (Material[])row.renderer.sharedMaterials.Clone();
                        try
                        {
                            int beforeGroups = operation.ivyGroups, beforeLeaves = operation.ivyLeaves;
                            item.ivy = ApplyIvy(row, own, map.transform, root,
                                row.policy.seed != 0 ? row.policy.seed : StableSeed(row.key), operation);
                            if (operation.ivyGroups != beforeGroups + 1 || operation.ivyLeaves <= beforeLeaves)
                                throw new InvalidOperationException("Ivy group had no positive leaf gain");
                            if (row.filter.sharedMesh != sourceMesh ||
                                !row.renderer.sharedMaterials.SequenceEqual(sourceMaterials))
                                throw new InvalidOperationException("Ivy changed source mesh/material bindings");
                            run.succeededIvyGroups++;
                            EditorUtility.SetDirty(row.renderer.gameObject);
                            EditorUtility.SetDirty(target.gameObject);
                            if (PrefabUtility.IsPartOfPrefabInstance(row.renderer))
                            {
                                PrefabUtility.RecordPrefabInstancePropertyModifications(row.filter);
                                PrefabUtility.RecordPrefabInstancePropertyModifications(row.renderer);
                                if (PrefabUtility.IsPartOfPrefabInstance(target))
                                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                            }
                        }
                        catch (Exception exception)
                        {
                            row.filter.sharedMesh = sourceMesh;
                            row.renderer.sharedMaterials = sourceMaterials;
                            item.ivy = "refine-failed: " + exception.Message;
                            errors.Add("ivy: " + exception.Message);
                        }
                        if (target == row.renderer.transform && own && own != row.renderer.transform &&
                            own.childCount == 0) Object.DestroyImmediate(own.gameObject);
                    }
                }
                if (errors.Count == 0)
                {
                    item.status = "treated";
                    item.detail = "";
                }
                else
                {
                    item.status = "failed";
                    item.detail = string.Join(" | ", errors);
                    failures.Add(row.key + ": " + item.detail);
                }
            }
        
            // Count actual positive-leaf markers, not the former fake 0-leaf groups.
            result.ivyGroups = 0;
            result.ivyLeaves = 0;
            foreach (Row row in representatives)
            {
                Transform target = string.IsNullOrEmpty(row.policy.ivyTargetPath) ?
                    row.renderer.transform : map.transform.Find(row.policy.ivyTargetPath);
                Transform host = target ? target.Find(OwnedRoot) : null;
                Transform ivy = host ? host.Find("__A33_Ivy") : null;
                var marker = ivy ? ivy.GetComponent<SurfaceIvyInstance>() : null;
                int leaves = marker ? LeafCount(marker.generationReport) : 0;
                if (leaves > 0) { result.ivyGroups++; result.ivyLeaves += leaves; }
                else
                {
                    var item = resultByKey[row.key];
                    if (item.status != "failed")
                    {
                        item.status = "failed";
                        item.detail = "No positive-leaf generated ivy marker after refinement";
                        failures.Add(row.key + ": " + item.detail);
                    }
                }
            }
        
            result.weatherSlots += operation.weatherSlots;
            result.protectedRows = result.rows.Count(r => r.status == "protected" || r.status == "skipped");
            result.treatedRows = result.rows.Count(r => r.status == "treated");
            result.unhandledRows = result.rows.Count(r => r.status == "unhandled");
            result.failedRows = result.rows.Count(r => r.status == "failed");
            run.failedIvyGroups = representatives.Count - run.succeededIvyGroups;
            run.ivyGroupsAfter = result.ivyGroups;
            run.ivyLeavesAfter = result.ivyLeaves;
            run.weatherSlotsAdded = operation.weatherSlots;
            run.generatedMeshesThisRun = operation.generatedMeshes;
            run.generatedMaterialsThisRun = operation.generatedMaterials;
            run.failures = failures.ToArray();
            run.finishedUtc = DateTime.UtcNow.ToString("O");
            result.summary = "rows=" + result.total + " treated=" + result.treatedRows +
                " protected=" + result.protectedRows + " unhandled=" + result.unhandledRows +
                " failed=" + result.failedRows + " weatherSlots=" + result.weatherSlots +
                " damageMeshes=" + result.damageMeshes + " ivyGroups=" + result.ivyGroups +
                " ivyLeaves=" + result.ivyLeaves + " ivyRefined=" + run.succeededIvyGroups + "/" +
                run.plannedIvyGroups + " newAssetsInRefineRun=" + operation.generatedMeshes + "/" +
                operation.generatedMaterials;
        
            EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
            if (string.IsNullOrEmpty(runReportJsonPath))
                runReportJsonPath = resultJsonPath + ".ivy-refine-run.json";
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(runReportJsonPath)));
            File.WriteAllText(runReportJsonPath, JsonUtility.ToJson(run, true));
            File.WriteAllText(resultJsonPath, JsonUtility.ToJson(result, true));
            return result;
        }

        public static RolloutResult ApplyVisuals(StageABlockoutMap map, RolloutPlan plan,
            string generatedAssetRoot = DefaultAssetRoot, string resultJsonPath = null)
        {
            if (!map || plan == null || plan.map != map) throw new ArgumentException("Plan/map mismatch");
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit mode required");
            if (plan.errors.Count != 0) throw new InvalidOperationException("Policy preflight failed:\n" + string.Join("\n", plan.errors.Take(30)));
            string root = AssetRoot(generatedAssetRoot);
            EnsureFolders(root);
            var baseline = LoadBaseline(root);
            var baselineByKey = baseline.rows.ToDictionary(r => r.key, StringComparer.Ordinal);
            var results = new List<RowResult>(plan.rows.Count);
            var totals = new RolloutResult { total = plan.rows.Count };
            var materialCache = new Dictionary<string, Material>(StringComparer.Ordinal);
            var ivyDone = new HashSet<string>(StringComparer.Ordinal);
            var weatherShader = Shader.Find(WeatherShader);
            if (!weatherShader) throw new InvalidOperationException("Missing " + WeatherShader);
            // Persist the complete reversible source recipe before mutating any renderer.
            foreach (Row row in plan.rows)
                if (!Protected(row.policy) && Eligible(row, out _) && !baselineByKey.ContainsKey(row.key))
                {
                    var source = new StageARuinRolloutBaselineRow { key=row.key,
                        sourceMesh=row.filter.sharedMesh, sourceMaterials=(Material[])row.renderer.sharedMaterials.Clone(),
                        sourceEnabled=row.renderer.enabled, sourcePropertyBlockOccupied=row.renderer.HasPropertyBlock() };
                    baseline.rows.Add(source); baselineByKey.Add(row.key,source);
                }
            EditorUtility.SetDirty(baseline);
            AssetDatabase.SaveAssetIfDirty(baseline);

            // The source renderer identity is retained. Generated children are owned by it.
            foreach (Row row in plan.rows.OrderBy(r => r.key, StringComparer.Ordinal))
            {
                var item = new RowResult { key = row.key, status = "protected", weather = "none", damage = "none", ivy = "none" };
                results.Add(item);
                if (results.Count % 100 == 0 && !string.IsNullOrEmpty(resultJsonPath))
                {
                    try
                    {
                        File.WriteAllText(resultJsonPath + ".progress.txt", DateTime.Now.ToString("O") +
                            " rows=" + results.Count + "/" + plan.rows.Count +
                            " treated=" + totals.treatedRows + " failures=" + totals.failedRows +
                            " ivy=" + totals.ivyGroups + " leaves=" + totals.ivyLeaves);
                    }
                    catch (IOException) { /* Progress is optional; readers must not abort the rollout. */ }
                }
                if (Protected(row.policy))
                {
                    if (baselineByKey.TryGetValue(row.key, out var prior) && row.renderer && row.filter)
                        Restore(row, prior);
                    totals.protectedRows++;
                    item.detail = Reason(row.policy);
                    continue;
                }
                if (!Eligible(row, out string issue))
                {
                    if (baselineByKey.TryGetValue(row.key, out var prior) && row.renderer && row.filter)
                        Restore(row, prior);
                    item.status = "skipped"; item.detail = issue; totals.protectedRows++;
                    continue;
                }
                if (!baselineByKey.TryGetValue(row.key, out var original))
                {
                    original = new StageARuinRolloutBaselineRow { key = row.key, sourceMesh = row.filter.sharedMesh,
                        sourceMaterials = (Material[])row.renderer.sharedMaterials.Clone(),
                        sourceEnabled = row.renderer.enabled,
                        sourcePropertyBlockOccupied = row.renderer.HasPropertyBlock() };
                    baseline.rows.Add(original); baselineByKey.Add(row.key, original);
                }
                try
                {
                    Restore(row, original);
                    var own = new GameObject(OwnedRoot);
                    own.transform.SetParent(row.renderer.transform, false);
                    int seed = row.policy.seed != 0 ? row.policy.seed : StableSeed(row.key);
                    int weatherBefore = totals.weatherSlots;
                    int damageBefore = totals.damageMeshes;
                    int ivyBefore = totals.ivyGroups;
                    item.damage = ApplyDamage(row, own.transform, root, seed, totals);
                    item.weather = original.sourcePropertyBlockOccupied
                        ? "source-property-block-protected" :
                        ApplyWeather(row, root, weatherShader, materialCache, seed, totals);
                    string group = string.IsNullOrEmpty(row.policy.ivyGroup) ? row.key : row.policy.ivyGroup;
                    if (Mode(row.policy.ivyMode, "Apply") && ivyDone.Add(group))
                        item.ivy = ApplyIvy(row, own.transform, map.transform, root, seed, totals);
                    bool changed = totals.damageMeshes > damageBefore ||
                        totals.weatherSlots > weatherBefore || totals.ivyGroups > ivyBefore;
                    if (changed)
                    {
                        if (own.transform.childCount == 0) Object.DestroyImmediate(own);
                        EditorUtility.SetDirty(row.filter);
                        EditorUtility.SetDirty(row.renderer);
                        EditorUtility.SetDirty(row.renderer.gameObject);
                        if (PrefabUtility.IsPartOfPrefabInstance(row.renderer))
                        {
                            PrefabUtility.RecordPrefabInstancePropertyModifications(row.filter);
                            PrefabUtility.RecordPrefabInstancePropertyModifications(row.renderer);
                        }
                        item.status = "treated"; totals.treatedRows++;
                    }
                    else
                    {
                        Object.DestroyImmediate(own);
                        item.status = "unhandled"; totals.unhandledRows++;
                        item.detail = Reason(row.policy);
                    }
                }
                catch (Exception exception)
                {
                    Restore(row, original);
                    item.status = "failed"; item.detail = exception.GetType().Name + ": " + exception.Message;
                    totals.failedRows++;
                }
            }
            EditorUtility.SetDirty(baseline);
            AssetDatabase.SaveAssetIfDirty(baseline);
            EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
            totals.rows = results.ToArray();
            totals.summary = "rows=" + totals.total + " treated=" + totals.treatedRows +
                " protected=" + totals.protectedRows + " unhandled=" + totals.unhandledRows +
                " failed=" + totals.failedRows +
                " weatherSlots=" + totals.weatherSlots + " damageMeshes=" + totals.damageMeshes +
                " ivyGroups=" + totals.ivyGroups + " ivyLeaves=" + totals.ivyLeaves;
            if (!string.IsNullOrEmpty(resultJsonPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(resultJsonPath)));
                File.WriteAllText(resultJsonPath, JsonUtility.ToJson(totals, true));
            }
            return totals;
        }

        static bool Eligible(Row row, out string issue)
        {
            issue = "";
            if (!row.renderer || !row.filter || !row.filter.sharedMesh) issue = "missing static MeshRenderer/MeshFilter";
            else if (row.renderer.GetComponentInParent<Rigidbody>()) issue = "dynamic rigidbody";
            else if (row.renderer.GetComponentInParent<SurfaceIvyInstance>()) issue = "existing ivy output";
            else if (row.renderer.name.StartsWith("__", StringComparison.Ordinal)) issue = "generated/proxy object";
            return issue.Length == 0;
        }
        public static void ProbeDamage(StageABlockoutMap map, string policyPath, string output)
        {
            var plan=Plan(map,policyPath); if(plan.errors.Count>0) throw new InvalidOperationException(string.Join("\n",plan.errors));
            EnsureFolders("Assets/Escape/Art/SurfaceRuin/StageARolloutProbes");
            var lines=new StringBuilder("UNIT_SCALE_DAMAGE_PROBES\n");
            var selected=new List<Row>();
            foreach (var term in new[]{"HALL_Roof_renderOpaque","HALL_Shell_renderOpaque","S06_Roof_renderOpaque","SW_W_SalvageCar","SW_W_Van"})
            {
                var row=plan.rows.FirstOrDefault(x=>x.key.Contains(term)&& !Protected(x.policy) &&
                    (Mode(x.policy.damageMode,"Apply")||Mode(x.policy.damageMode,"SafeChip")));
                if(row==null){lines.AppendLine("MISSING "+term);continue;}
                selected.Add(row);
            }
            foreach(var row in selected)
            {
                Scene scene;var copy=IsolatedCopy(row.renderer,out scene);
                try
                {
                    var own=new GameObject(OwnedRoot);own.transform.SetParent(copy.transform,false);
                    var working=new Row{key=row.key,renderer=copy.GetComponent<MeshRenderer>(),filter=copy.GetComponent<MeshFilter>(),policy=row.policy};
                    var totals=new RolloutResult();
                    lines.AppendLine(row.key+"\n"+ApplyDamage(working,own.transform,"Assets/Escape/Art/SurfaceRuin/StageARolloutProbes",row.policy.seed,totals));
                }
                catch(Exception e){lines.AppendLine("ERROR "+e);}
                finally{if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);}
            }
            File.WriteAllText(output,lines.ToString());
        }
        public static void ProbeIvy(StageABlockoutMap map,string policyPath,string output)
        {
            var plan=Plan(map,policyPath);EnsureFolders("Assets/Escape/Art/SurfaceRuin/StageARolloutProbes");
            var text=new StringBuilder("WORLD_METRE_EXCLUSION_IVY_PROBES\n");
            foreach(var term in new[]{"CLINIC_Shell_renderOpaque","S06_Shell_renderOpaque","W05_Shell_renderOpaque"})
            {
                var row=plan.rows.First(x=>x.key.Contains(term)&&Mode(x.policy.ivyMode,"Apply"));
                Scene scene;var copy=IsolatedCopy(row.renderer,out scene);
                try
                {
                    var own=new GameObject(OwnedRoot);own.transform.SetParent(copy.transform,false);
                    var localPolicy=JsonUtility.FromJson<PolicyEntry>(JsonUtility.ToJson(row.policy));
                    localPolicy.ivyTargetPath=null;
                    var working=new Row{key=row.key,renderer=copy.GetComponent<MeshRenderer>(),filter=copy.GetComponent<MeshFilter>(),policy=localPolicy};
                    text.AppendLine(row.key+"\n"+ApplyIvy(working,own.transform,copy.transform.root,"Assets/Escape/Art/SurfaceRuin/StageARolloutProbes",row.policy.seed,new RolloutResult()));
                }
                catch(Exception e){text.AppendLine("ERROR "+e);}
                finally{if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);}
            }
            File.WriteAllText(output,text.ToString());
        }

        static string ApplyDamage(Row row, Transform own, string root, int seed, RolloutResult totals)
        {
            if (!Mode(row.policy.damageMode, "Apply") && !Mode(row.policy.damageMode, "SafeChip")) return "policy-off";
            // Match GeometryEngine's ellipsoid exactly for buildings. Its wobble
            // is bounded by +/- .075, so sqrt(1.075) bounds every removed point.
            // This rejects all cuts that could touch the traversal band, even
            // with rotated or nonuniformly scaled renderer ancestors.
            float minY = row.policy.minCutWorldY != 0f ? row.policy.minCutWorldY :
                row.policy.floorWorldY + (row.policy.minCutHeight > 0 ? row.policy.minCutHeight : 2.1f);
            float damage = Mode(row.policy.damageMode, "SafeChip")
                ? Mathf.Clamp(row.policy.maxDamage, .06f, .16f)
                : Mathf.Clamp(row.policy.maxDamage, .06f, .55f);
            float predictedLowestCutY = StageARuinRolloutGeometryCarrier.LowestPossibleCutWorldY(
                row.renderer.bounds, Kind(row.policy.assetClass), damage);
            if (predictedLowestCutY < minY) return "height-gated; predicted=" +
                predictedLowestCutY.ToString("F2") + "; limit=" + minY.ToString("F2");
            if (row.filter.sharedMesh.vertexCount < 24) return "low-detail-gated";
            var settings = new RuinGeometrySettings { seed = seed,
                kind = Kind(row.policy.assetClass), damage = damage,
                debrisAmount = 0f, fragmentBudget = 0, preserveOpenings = true,
                adaptBuildingCutToSurface = true,
                triangleBudget = 250000 };
            Scene scene; GameObject copy = IsolatedCopy(row.renderer, out scene);
            GeometryReport report = null;
            try
            {
                report = RuinGeometryEngine.Apply(copy.transform.root.gameObject, settings);
                if (!report.geometryChanged || !report.generatedRoot) return "no-intersection; " + report;
                var cut = report.generatedRoot.GetComponentsInChildren<MeshRenderer>(true)
                    .FirstOrDefault(r => r.name == "__RuinGenerated_" + row.renderer.name);
                if (!cut || !cut.GetComponent<MeshFilter>()) return "cut-output-missing";
                var cutMesh = cut.GetComponent<MeshFilter>().sharedMesh;
                CutAudit audit;
                Mesh convertedCut = StageARuinRolloutGeometryCarrier.ToSourceLocal(
                    cutMesh, cut.transform, row.renderer.transform);
                try
                {
                    audit = AuditCutHeight(row.filter.sharedMesh, convertedCut,
                        row.renderer.transform.localToWorldMatrix, minY);
                    if (!audit.pass) return "cut-audit-rejected; " + audit.report;
                    row.filter.sharedMesh = SaveMesh(convertedCut, root, row.key + "|cut", totals);
                }
                finally { if (convertedCut) Object.DestroyImmediate(convertedCut); }
                row.renderer.sharedMaterials = PersistentMaterials(cut.sharedMaterials, root,
                    row.key + "|cut", totals);
                foreach (var generated in report.generatedRoot.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (generated == cut) continue;
                    var sourceFilter = generated.GetComponent<MeshFilter>();
                    if (!sourceFilter || !sourceFilter.sharedMesh) continue;
                    var child = new GameObject("__A33_" + generated.name);
                    child.transform.SetParent(own, false);
                    child.layer = row.renderer.gameObject.layer;
                    Mesh convertedExtra = StageARuinRolloutGeometryCarrier.ToSourceLocal(
                        sourceFilter.sharedMesh, generated.transform, row.renderer.transform);
                    try
                    {
                        child.AddComponent<MeshFilter>().sharedMesh = SaveMesh(convertedExtra, root,
                            row.key + "|" + generated.name, totals);
                    }
                    finally { if (convertedExtra) Object.DestroyImmediate(convertedExtra); }
                    var renderer = child.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials = PersistentMaterials(generated.sharedMaterials, root,
                        row.key + "|" + generated.name, totals);
                    renderer.shadowCastingMode = generated.shadowCastingMode;
                    renderer.receiveShadows = generated.receiveShadows;
                    renderer.renderingLayerMask = generated.renderingLayerMask;
                }
                totals.damageMeshes++;
                return "cut; " + audit.report + "; sourceTriangles=" + report.sourceTriangles +
                    "; resultTriangles=" + report.resultTriangles;
            }
            finally
            {
                try { if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene); }
                finally
                {
                    if (report != null)
                    {
                        foreach (Mesh mesh in report.createdMeshes)
                            if (mesh && !EditorUtility.IsPersistent(mesh)) Object.DestroyImmediate(mesh);
                        foreach (Material material in report.createdMaterials)
                            if (material && !EditorUtility.IsPersistent(material)) Object.DestroyImmediate(material);
                    }
                }
            }
        }

        static string ApplyWeather(Row row, string root, Shader shader,
            Dictionary<string, Material> cache, int seed, RolloutResult totals)
        {
            if (!Mode(row.policy.weatheringMode, "Tool") &&
                !Mode(row.policy.weatheringMode, "CustomVariantOrOverlay")) return "policy-off";
            var source = row.renderer.sharedMaterials;
            var next = (Material[])source.Clone();
            int tool = 0, native = 0, unsupported = 0, protectedSlots = 0;
            for (int slot = 0; slot < source.Length; ++slot)
            {
                Material material = source[slot];
                if (!material || !material.shader) { unsupported++; continue; }
                if (ProtectedWeatherSlot(material, row.renderer.name)) { protectedSlots++; continue; }
                bool toolEligible = WeatherEligible(material, row.renderer.name);
                bool nativeEligible = material.HasProperty("_Wetness") &&
                    material.HasProperty("_AlbedoContrast") && material.HasProperty("_WetDarkening");
                bool clinicNative = material.shader.name == "Escape/ClinicV32/Surface" &&
                    material.HasProperty("_Mode") && material.GetFloat("_Mode") > .5f;
                nativeEligible |= clinicNative;
                if (!toolEligible && !nativeEligible) { unsupported++; continue; }
                string mode = toolEligible ? "tool" : "native";
                string materialKey = AssetDatabase.GetAssetPath(material) + "|" + material.name + "|" +
                    row.policy.assetClass + "|" + row.policy.surfaceClass + "|" + mode + "|" +
                    ((seed & int.MaxValue) % 4);
                if (!cache.TryGetValue(materialKey, out var aged))
                {
                    string path = root + "/Materials/" + (toolEligible ? "W_" : "N_") +
                        Digest(materialKey) + ".mat";
                    aged = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (!aged)
                    {
                        aged = new Material(material) { name = material.name +
                            (toolEligible ? "_A33Weathered" : "_A33NativeAged") };
                        if (toolEligible)
                        {
                            aged.shader = material.shader;
                            aged.CopyPropertiesFromMaterial(material);
                            aged.shader = shader;
                            aged.shaderKeywords = material.shaderKeywords;
                            WeatheringSettings recipe = Recipe(row.policy.assetClass, row.policy.surfaceClass);
                            SetWeather(aged, recipe, WeatherKind(material), seed);
                            aged.SetFloat("_WeatherPreserveD09", material.shader.name == D09Shader ? 1f : 0f);
                        }
                        else
                        {
                            if (clinicNative)
                            {
                                aged.SetFloat("_TextureInfluence", Mathf.Min(.85f,material.GetFloat("_TextureInfluence") + .18f));
                                aged.SetFloat("_Smoothness", material.GetFloat("_Smoothness") * .8f);
                            }
                            else
                            {
                                aged.SetFloat("_Wetness", Mathf.Clamp01(material.GetFloat("_Wetness") + .035f));
                                aged.SetFloat("_WetDarkening", Mathf.Clamp(material.GetFloat("_WetDarkening") + .025f, 0f, .25f));
                                aged.SetFloat("_AlbedoContrast", Mathf.Clamp(material.GetFloat("_AlbedoContrast") - .035f, .35f, 1f));
                            }
                        }
                        aged.renderQueue = material.renderQueue;
                        aged.enableInstancing = material.enableInstancing;
                        AssetDatabase.CreateAsset(aged, path);
                        AssetDatabase.SaveAssetIfDirty(aged);
                        totals.generatedMaterials++;
                    }
                    if (toolEligible && row.policy.surfaceClass == "Roof")
                    {
                        SetWeather(aged, Recipe(row.policy.assetClass,row.policy.surfaceClass), WeatherKind(material), seed);
                        aged.SetFloat("_WeatherPreserveD09",material.shader.name == D09Shader ? 1f : 0f);
                        EditorUtility.SetDirty(aged); AssetDatabase.SaveAssetIfDirty(aged);
                    }
                    cache.Add(materialKey, aged);
                }
                next[slot] = aged;
                if (toolEligible) tool++; else native++;
            }
            if (tool + native > 0)
            {
                row.renderer.sharedMaterials = next;
                if (tool > 0)
                {
                    var binding = row.renderer.GetComponent<AMapRuinWeatheringBinding>();
                    if (!binding) binding = row.renderer.gameObject.AddComponent<AMapRuinWeatheringBinding>();
                    binding.Configure(row.renderer,
                        row.renderer.bounds.min.y - row.renderer.transform.position.y,
                        row.renderer.bounds.size.y, seed);
                }
            }
            totals.weatherSlots += tool + native;
            return "tool-aged-slots=" + tool + "; native-already-aged-enhanced=" + native +
                "; unsupported-slots=" + unsupported + "; protected-slots=" + protectedSlots;
        }

        static float LowestPossibleCutY(MeshRenderer renderer, RuinAssetKind kind,
            float damage, int seed)
        {
            Bounds world = renderer.bounds;
            Bounds local = new Bounds();
            bool first = true;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 point = renderer.transform.InverseTransformPoint(world.center +
                    Vector3.Scale(world.extents, new Vector3(x, y, z)));
                if (first) { local = new Bounds(point, Vector3.zero); first = false; }
                else local.Encapsulate(point);
            }
            Vector3 s = local.size;
            bool px = (seed & 1) != 0, pz = (seed & 2) != 0;
            Vector3 center, radius;
            if (kind == RuinAssetKind.Building)
            {
                center = new Vector3(px ? local.max.x - .015f * s.x : local.min.x + .015f * s.x,
                    local.max.y - .06f * s.y,
                    pz ? local.max.z - .015f * s.z : local.min.z + .015f * s.z);
                radius = new Vector3(Mathf.Max(.30f, s.x * (.16f + .18f * damage)),
                    Mathf.Max(.35f, s.y * (.28f + .30f * damage)),
                    Mathf.Max(.30f, s.z * (.16f + .19f * damage)));
            }
            else if (kind == RuinAssetKind.Vehicle)
            {
                center = new Vector3(px ? local.max.x - .07f * s.x : local.min.x + .07f * s.x,
                    local.max.y - .18f * s.y,
                    pz ? local.max.z - .12f * s.z : local.min.z + .12f * s.z);
                radius = new Vector3(Mathf.Max(.12f, s.x * (.24f + .15f * damage)),
                    Mathf.Max(.10f, s.y * (.28f + .26f * damage)),
                    Mathf.Max(.12f, s.z * (.25f + .18f * damage)));
            }
            else
            {
                center = new Vector3(px ? local.max.x - .05f * s.x : local.min.x + .05f * s.x,
                    local.max.y - .08f * s.y,
                    pz ? local.max.z - .05f * s.z : local.min.z + .05f * s.z);
                radius = new Vector3(Mathf.Max(.06f, s.x * (.27f + .15f * damage)),
                    Mathf.Max(.06f, s.y * (.28f + .22f * damage)),
                    Mathf.Max(.06f, s.z * (.27f + .15f * damage)));
            }
            Matrix4x4 m = renderer.localToWorldMatrix;
            float ry = Mathf.Sqrt(Mathf.Pow(m.m10 * radius.x, 2f) +
                Mathf.Pow(m.m11 * radius.y, 2f) + Mathf.Pow(m.m12 * radius.z, 2f));
            // Non-building AdaptToSurface can move the centre slightly down.
            float extra = kind == RuinAssetKind.Building ? 0f : .15f * ry;
            return m.MultiplyPoint3x4(center).y - Mathf.Sqrt(1.075f) * ry - extra;
        }

        sealed class CutAudit
        {
            public bool pass;
            public string report;
        }

        sealed class MeshAuditData
        {
            public Vector3[] vertices;
            public readonly List<int[]> triangles = new List<int[]>();
        }

        // Verifies the actual output, in addition to the mathematical ellipsoid
        // bound. Every source triangle touching the protected band must survive
        // byte-equivalent within a 1 mm position grid, and no newly created
        // vertex may enter that band. A failed audit discards the entire cut.
        static CutAudit AuditCutHeight(Mesh source, Mesh result, Matrix4x4 world, float limit)
        {
            MeshAuditData before = ReadAuditMesh(source), after = ReadAuditMesh(result);
            var originalPoints = new HashSet<Vector3Int>();
            foreach (Vector3 p in before.vertices) originalPoints.Add(Quantize(world.MultiplyPoint3x4(p)));
            int newVertices = 0;
            float minNewY = float.PositiveInfinity;
            foreach (Vector3 p in after.vertices)
            {
                Vector3 position = world.MultiplyPoint3x4(p);
                if (originalPoints.Contains(Quantize(position))) continue;
                newVertices++;
                minNewY = Mathf.Min(minNewY, position.y);
            }
            var lowBefore = LowTriangles(before, world, limit);
            var lowAfter = LowTriangles(after, world, limit);
            bool identical = lowBefore.Count == lowAfter.Count &&
                lowBefore.All(pair => lowAfter.TryGetValue(pair.Key, out int count) && count == pair.Value);
            bool newAbove = newVertices == 0 || minNewY >= limit - .001f;
            var strict = new CutAudit { pass = identical && newAbove,
                report = "cutLimitWorldY=" + limit.ToString("F3") +
                    "; actualNewVertexMinWorldY=" +
                    (newVertices == 0 ? "none" : minNewY.ToString("F3")) +
                    "; newVertices=" + newVertices +
                    "; lowTrianglesBefore=" + lowBefore.Values.Sum() +
                    "; lowTrianglesAfter=" + lowAfter.Values.Sum() +
                    "; lowTriangleExact=" + identical };
            if (strict.pass) return strict;
            var spatial = StageARuinRolloutCutAudit.Check(before.vertices,before.triangles,
                after.vertices,after.triangles,world,limit);
            if (spatial.pass) return new CutAudit { pass=true,
                report="strict=["+strict.report+"]; spatial=["+spatial.report+"]" };
            var surface = StageARuinRolloutSurfaceAudit.Check(before.vertices,before.triangles,
                after.vertices,after.triangles,world,limit);
            return new CutAudit { pass=surface.pass, report="strict=["+strict.report+
                "]; spatial=["+spatial.report+"]; surface=["+surface.report+"]" };
        }

        static Dictionary<string, int> LowTriangles(MeshAuditData mesh, Matrix4x4 world, float limit)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (int[] indices in mesh.triangles)
            for (int i = 0; i + 2 < indices.Length; i += 3)
            {
                Vector3 a = world.MultiplyPoint3x4(mesh.vertices[indices[i]]);
                Vector3 b = world.MultiplyPoint3x4(mesh.vertices[indices[i + 1]]);
                Vector3 c = world.MultiplyPoint3x4(mesh.vertices[indices[i + 2]]);
                if (Mathf.Min(a.y, Mathf.Min(b.y, c.y)) >= limit) continue;
                var points = new[] { Quantize(a), Quantize(b), Quantize(c) };
                Array.Sort(points, (x, y) => x.x != y.x ? x.x.CompareTo(y.x) :
                    x.y != y.y ? x.y.CompareTo(y.y) : x.z.CompareTo(y.z));
                string key = points[0] + "|" + points[1] + "|" + points[2];
                result.TryGetValue(key, out int n); result[key] = n + 1;
            }
            return result;
        }

        static Vector3Int Quantize(Vector3 p) => new Vector3Int(
            Mathf.RoundToInt(p.x * 1000f), Mathf.RoundToInt(p.y * 1000f),
            Mathf.RoundToInt(p.z * 1000f));

        static MeshAuditData ReadAuditMesh(Mesh mesh)
        {
            var output = new MeshAuditData();
            using (var access = Mesh.AcquireReadOnlyMeshData(mesh))
            {
                var data = access[0];
                if (!data.HasVertexAttribute(VertexAttribute.Position))
                    throw new InvalidOperationException("Cut audit source has no position attribute");
                var format = data.GetVertexAttributeFormat(VertexAttribute.Position);
                if (format != VertexAttributeFormat.Float32 && format != VertexAttributeFormat.Float16)
                    throw new NotSupportedException("Cut audit position format: " + format);
                int stream = data.GetVertexAttributeStream(VertexAttribute.Position);
                int stride = data.GetVertexBufferStride(stream);
                int offset = data.GetVertexAttributeOffset(VertexAttribute.Position);
                int size = format == VertexAttributeFormat.Float32 ? 4 : 2;
                var bytes = data.GetVertexData<byte>(stream);
                output.vertices = new Vector3[data.vertexCount];
                for (int i = 0; i < output.vertices.Length; ++i)
                {
                    int position = i * stride + offset;
                    output.vertices[i] = new Vector3(
                        ReadComponent(bytes, position, format),
                        ReadComponent(bytes, position + size, format),
                        ReadComponent(bytes, position + size * 2, format));
                }
                bool wide = data.indexFormat == IndexFormat.UInt32;
                NativeArray<uint> index32 = default;
                NativeArray<ushort> index16 = default;
                if (wide) index32 = data.GetIndexData<uint>();
                else index16 = data.GetIndexData<ushort>();
                for (int submesh = 0; submesh < data.subMeshCount; ++submesh)
                {
                    var sub = data.GetSubMesh(submesh);
                    if (sub.topology != MeshTopology.Triangles) continue;
                    var triangles = new int[sub.indexCount];
                    for (int i = 0; i < triangles.Length; ++i)
                        triangles[i] = (int)(wide ? index32[sub.indexStart + i] :
                            index16[sub.indexStart + i]) + sub.baseVertex;
                    output.triangles.Add(triangles);
                }
            }
            return output;
        }

        static float ReadComponent(NativeArray<byte> bytes, int p, VertexAttributeFormat format)
        {
            if (format == VertexAttributeFormat.Float32)
                return BitConverter.ToSingle(new[] { bytes[p], bytes[p + 1], bytes[p + 2], bytes[p + 3] }, 0);
            int half = bytes[p] | bytes[p + 1] << 8;
            int sign = (half & 0x8000) << 16, exponent = (half >> 10) & 31, mantissa = half & 1023;
            int bits;
            if (exponent == 0)
            {
                if (mantissa == 0) bits = sign;
                else
                {
                    exponent = 1;
                    while ((mantissa & 1024) == 0) { mantissa <<= 1; exponent--; }
                    mantissa &= 1023;
                    bits = sign | (exponent + 112) << 23 | mantissa << 13;
                }
            }
            else if (exponent == 31) bits = sign | 0x7F800000 | mantissa << 13;
            else bits = sign | (exponent + 112) << 23 | mantissa << 13;
            return BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
        }

        static bool ProtectedWeatherSlot(Material material, string rendererName)
        {
            if (!material || !material.shader || material.renderQueue >= (int)RenderQueue.Transparent ||
                (material.HasProperty("_Surface") && material.GetFloat("_Surface") > .5f)) return true;
            string m = material.name.ToLowerInvariant(), r = rendererName.ToLowerInvariant();
            if (m.Contains("wheel") && !(m + r).Contains("flywheel")) return true;
            if (new[] { "glass", "pane", "transparent", "tire", "tyre", "rubber",
                "wood", "timber", "cloth", "fabric", "foliage", "leaf",
                "grass", "ivy", "particle", "overlay", "stain" }.Any(m.Contains)) return true;
            return new[] { "glass", "tire", "tyre", "rubber", "foliage", "ivy",
                "particle", "collideronly", "_collider", "collision" }.Any(r.Contains);
        }

        static string ApplyIvy(Row row, Transform own, Transform mapRoot,
            string root, int seed, RolloutResult totals)
        {
            Transform target = string.IsNullOrEmpty(row.policy.ivyTargetPath) ?
                row.renderer.transform : mapRoot.Find(row.policy.ivyTargetPath);
            if (!target) throw new InvalidOperationException("Ivy target path missing: " + row.policy.ivyTargetPath);
            bool createdHost = false;
            if (target != row.renderer.transform)
            {
                var host = target.Find(OwnedRoot);
                if (!host)
                {
                    var created = new GameObject(OwnedRoot);
                    created.transform.SetParent(target, false);
                    host = created.transform;
                    createdHost = true;
                }
                own = host;
            }
            Scene scene; GameObject copy = target == row.renderer.transform ?
                IsolatedCopy(row.renderer, out scene) : IsolatedVisualTarget(target, out scene);
            SurfaceIvyInstance preview = null;
            GameObject pendingIvy = null;
            try
            {
                string kind = row.policy.assetClass == "Vehicle" ? "Vehicle" :
                    row.policy.assetClass == "Building" ? "Building" : "Prop";
                var recipe = RuinIvyRecipes.ForTarget(copy, kind, seed, .68f);
                recipe.stopAtLeafBudget = true;
                recipe.roots = Mathf.Min(recipe.roots, kind == "Building" ? 32 : kind == "Vehicle" ? 18 : 9);
                recipe.steps = Mathf.Min(recipe.steps, kind == "Building" ? 130 : 90);
                recipe.maxBranches = Mathf.Min(recipe.maxBranches, kind == "Building" ? 55 : 28);
                recipe.maxLeaves = Mathf.Min(recipe.maxLeaves,
                    row.policy.ivyLeafBudget > 0 ? row.policy.ivyLeafBudget :
                    kind == "Building" ? 1100 : kind == "Vehicle" ? 700 : 250);
                var exclusions = LocalExclusions(copy.transform, row.policy.ivyExclusionsWorld);
                preview = SurfaceIvyAuthoring.Preview(copy, recipe,
                    new List<Vector3>(), exclusions);
                if (LeafCount(preview.generationReport) <= 0)
                    throw new InvalidOperationException("Ivy generated no leaves: " + preview.generationReport);
                var ivy = new GameObject("__A33_Ivy_Pending");
                pendingIvy = ivy;
                ivy.transform.SetParent(own, false);
                ivy.layer = row.renderer.gameObject.layer;
                var marker = ivy.AddComponent<SurfaceIvyInstance>();
                marker.sourceTarget = target;
                marker.sourceRenderers = target == row.renderer.transform ?
                    new Renderer[] { row.renderer } :
                    target.GetComponentsInChildren<MeshRenderer>(true)
                        .Where(r => r && r.enabled && r.GetComponent<MeshFilter>() &&
                            r.shadowCastingMode != ShadowCastingMode.ShadowsOnly &&
                            ActiveRelative(r.transform, target) && !OwnedAncestor(r.transform, target))
                        .Cast<Renderer>().Take(64).ToArray();
                marker.settings = recipe;
                marker.exclusions = exclusions;
                marker.isPreview = false;
                marker.followSourceVisibility = true;
                int meshes = 0;
                foreach (var sourceFilter in preview.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (!sourceFilter.sharedMesh) continue;
                    var sourceRenderer = sourceFilter.GetComponent<MeshRenderer>();
                    var child = new GameObject(sourceFilter.name);
                    child.transform.SetParent(ivy.transform, false);
                    child.layer = ivy.layer;
                    child.AddComponent<MeshFilter>().sharedMesh = SaveMesh(sourceFilter.sharedMesh,
                        root, row.key + "|ivy|" + sourceFilter.name, totals);
                    var renderer = child.AddComponent<MeshRenderer>();
                    if (sourceRenderer)
                    {
                        renderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
                        renderer.receiveShadows = sourceRenderer.receiveShadows;
                        renderer.lightProbeUsage = sourceRenderer.lightProbeUsage;
                        renderer.reflectionProbeUsage = sourceRenderer.reflectionProbeUsage;
                        renderer.renderingLayerMask = sourceRenderer.renderingLayerMask;
                        renderer.probeAnchor = sourceRenderer.probeAnchor;
                        renderer.lightProbeProxyVolumeOverride = sourceRenderer.lightProbeProxyVolumeOverride;
                        renderer.allowOcclusionWhenDynamic = sourceRenderer.allowOcclusionWhenDynamic;
                        renderer.motionVectorGenerationMode = sourceRenderer.motionVectorGenerationMode;
                        renderer.sortingLayerID = sourceRenderer.sortingLayerID;
                        renderer.sortingOrder = sourceRenderer.sortingOrder;
                    }
                    Material sourceMaterial = sourceRenderer ? sourceRenderer.sharedMaterial : null;
                    renderer.sharedMaterial = PersistentIvyMaterial(sourceMaterial, root,
                        sourceFilter.name, totals);
                    meshes++;
                }
                marker.stemMaterial = AssetDatabase.LoadAssetAtPath<Material>(root + "/Materials/IvyStem.mat");
                marker.leafMaterial = AssetDatabase.LoadAssetAtPath<Material>(root + "/Materials/IvyLeaf.mat");
                marker.generationReport = preview.generationReport;
                marker.RefreshVisibility();
                if (kind == "Building")
                    ivy.AddComponent<AMapRuinIvyRevealBinding>().Configure(marker);
                for (int i = own.childCount - 1; i >= 0; --i)
                {
                    Transform oldIvy = own.GetChild(i);
                    if (oldIvy != ivy.transform && oldIvy.name == "__A33_Ivy")
                        Object.DestroyImmediate(oldIvy.gameObject);
                }
                ivy.name = "__A33_Ivy";
                pendingIvy = null;
                totals.ivyGroups++;
                totals.ivyLeaves += preview.generationReport == null ? 0 : LeafCount(preview.generationReport);
                return "meshes=" + meshes + "; " + marker.generationReport;
            }
            finally
            {
                if (pendingIvy) Object.DestroyImmediate(pendingIvy);
                try { if (preview) SurfaceIvyAuthoring.Clear(preview); }
                finally { if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene); }
                if (createdHost && own && own.childCount == 0)
                    Object.DestroyImmediate(own.gameObject);
            }
        }

        static List<IvyExclusionBox> LocalExclusions(Transform target, WorldExclusion[] world)
        {
            var result = new List<IvyExclusionBox>();
            if (world == null) return result;
            foreach (var region in world)
            {
                Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
                Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
                for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 point = target.InverseTransformPoint(region.center +
                        Vector3.Scale((region.size + Vector3.one * .18f) * .5f, new Vector3(x, y, z)));
                    min = Vector3.Min(min, point); max = Vector3.Max(max, point);
                }
                result.Add(new IvyExclusionBox { center = (min + max) * .5f,
                    size = max - min, rotation = Vector3.zero });
            }
            return result;
        }

        static Material PersistentIvyMaterial(Material source, string root, string role, RolloutResult totals)
        {
            if (!source) return null;
            bool leaf = role.IndexOf("Leaf", StringComparison.OrdinalIgnoreCase) >= 0 ||
                role.IndexOf("Leav", StringComparison.OrdinalIgnoreCase) >= 0;
            string path = root + "/Materials/Ivy" + (leaf ? "Leaf" : "Stem") + ".mat";
            var saved = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (saved) return saved;
            saved = new Material(source) { name = "A33_Ivy_" + (leaf ? "Leaf" : "Stem") };
            Texture texture = source.mainTexture;
            if (texture is Texture2D image && !EditorUtility.IsPersistent(image))
            {
                string texturePath = root + "/Materials/IvyLeafTexture.asset";
                var savedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (!savedTexture)
                {
                    savedTexture = Object.Instantiate(image);
                    savedTexture.name = "A33_IvyLeafTexture";
                    AssetDatabase.CreateAsset(savedTexture, texturePath);
                    AssetDatabase.SaveAssetIfDirty(savedTexture);
                }
                saved.mainTexture = savedTexture;
            }
            AssetDatabase.CreateAsset(saved, path);
            AssetDatabase.SaveAssetIfDirty(saved);
            totals.generatedMaterials++;
            return saved;
        }

        static int LeafCount(string report)
        {
            var match = System.Text.RegularExpressions.Regex.Match(report, @"(?<n>\d+) leaves");
            return match.Success && int.TryParse(match.Groups["n"].Value, out int n) ? n : 0;
        }

        static bool WeatherEligible(Material m, string rendererName)
        {
            if (!m || !m.shader) return false;
            string shader = m.shader.name;
            if (shader != UrpShader && shader != D09Shader && shader != "Standard" &&
                shader != "Escape/SubcultureD15/EnvironmentLit") return false;
            if (m.renderQueue >= (int)RenderQueue.Transparent ||
                (m.HasProperty("_Surface") && m.GetFloat("_Surface") > .5f)) return false;
            string materialName = m.name.ToLowerInvariant();
            string objectName = rendererName.ToLowerInvariant();
            if (materialName.Contains("wheel") && !(materialName + objectName).Contains("flywheel")) return false;
            if (new[] { "glass", "pane", "transparent", "tire", "tyre",
                "rubber", "wood", "timber", "cloth", "fabric", "foliage",
                "leaf", "grass", "ivy", "particle", "overlay", "stain", "collision" }
                .Any(materialName.Contains)) return false;
            return !new[] { "glass", "tire", "tyre", "rubber", "foliage",
                "ivy", "particle", "collideronly", "_collider", "collision" }
                .Any(objectName.Contains);
        }

        static int WeatherKind(Material material)
        {
            string n = material.name.ToLowerInvariant();
            if (n.Contains("slate") || n.Contains("sheet") || n.Contains("steel") || n.Contains("patina")) return 2;
            if (n.Contains("brick") || n.Contains("concrete") || n.Contains("stone") ||
                n.Contains("masonry") || n.Contains("terracotta") || n.Contains("roof")) return 1;
            if (n.Contains("metal") || n.Contains("steel") || n.Contains("iron") ||
                n.Contains("rust") || n.Contains("sheet") || n.Contains("patina") ||
                (material.HasProperty("_Metallic") && material.GetFloat("_Metallic") > .35f)) return 2;
            return 0;
        }

        static WeatheringSettings Recipe(string assetClass, string surfaceClass)
        {
            if (surfaceClass == "Roof") return new WeatheringSettings {
                strength=.42f,rust=.20f,dirt=.25f,streaks=.30f,moss=.08f,fading=.20f,noiseScale=2.4f };
            if (assetClass == "Vehicle") return WeatheringSettings.Vehicle();
            if (surfaceClass != null && (surfaceClass.IndexOf("metal", StringComparison.OrdinalIgnoreCase) >= 0 ||
                surfaceClass.IndexOf("industrial", StringComparison.OrdinalIgnoreCase) >= 0))
                return WeatheringSettings.Painted();
            return assetClass == "Building" ? WeatheringSettings.Masonry() : WeatheringSettings.Painted();
        }

        static void SetWeather(Material m, WeatheringSettings p, int kind, int seed)
        {
            m.SetFloat("_WeatherStrength", p.strength);
            m.SetFloat("_WeatherRust", p.rust);
            m.SetFloat("_WeatherDirt", p.dirt);
            m.SetFloat("_WeatherStreaks", p.streaks);
            m.SetFloat("_WeatherMoss", p.moss);
            m.SetFloat("_WeatherFading", p.fading);
            m.SetFloat("_WeatherScale", p.noiseScale);
            m.SetFloat("_WeatherSeed", seed & 0x00FFFFFF);
            m.SetFloat("_WeatherMinY", 0f);
            m.SetFloat("_WeatherHeight", 1f);
            m.SetFloat("_WeatherMaterial", kind);
        }

        static GameObject IsolatedCopy(MeshRenderer source, out Scene scene)
        {
            scene = EditorSceneManager.NewPreviewScene();
            var chain = new Stack<Transform>();
            for (Transform t = source.transform.parent; t; t = t.parent) chain.Push(t);
            var root = new GameObject("A33_Isolated_Root");
            SceneManager.MoveGameObjectToScene(root, scene);
            Transform parent = root.transform;
            while (chain.Count != 0)
            {
                Transform original = chain.Pop();
                var next = new GameObject(original.name);
                next.transform.SetParent(parent, false);
                next.transform.localPosition = original.localPosition;
                next.transform.localRotation = original.localRotation;
                next.transform.localScale = original.localScale;
                parent = next.transform;
            }
            var copy = new GameObject(source.name);
            copy.transform.SetParent(parent, false);
            copy.transform.localPosition = source.transform.localPosition;
            copy.transform.localRotation = source.transform.localRotation;
            copy.transform.localScale = source.transform.localScale;
            copy.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
            var renderer = copy.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = source.sharedMaterials;
            renderer.shadowCastingMode = source.shadowCastingMode;
            renderer.receiveShadows = source.receiveShadows;
            renderer.renderingLayerMask = source.renderingLayerMask;
            return copy;
        }

        static GameObject IsolatedVisualTarget(Transform target, out Scene scene)
        {
            scene = EditorSceneManager.NewPreviewScene();
            var chain = new Stack<Transform>();
            for (Transform t = target.parent; t; t = t.parent) chain.Push(t);
            var root = new GameObject("A33_Isolated_Root");
            SceneManager.MoveGameObjectToScene(root, scene);
            Transform parent = root.transform;
            while (chain.Count != 0)
            {
                Transform original = chain.Pop();
                var next = new GameObject(original.name);
                next.transform.SetParent(parent, false);
                next.transform.localPosition = original.localPosition;
                next.transform.localRotation = original.localRotation;
                next.transform.localScale = original.localScale;
                parent = next.transform;
            }
            return CloneVisual(target, parent, true).gameObject;
        }

        static Transform CloneVisual(Transform original, Transform parent, bool forceActive = false)
        {
            var copy = new GameObject(original.name);
            copy.transform.SetParent(parent, false);
            copy.transform.localPosition = original.localPosition;
            copy.transform.localRotation = original.localRotation;
            copy.transform.localScale = original.localScale;
            copy.SetActive(forceActive || original.gameObject.activeSelf);
            var sourceRenderer = original.GetComponent<MeshRenderer>();
            var sourceFilter = original.GetComponent<MeshFilter>();
            if (sourceRenderer && sourceFilter && sourceFilter.sharedMesh)
            {
                copy.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
                var renderer = copy.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = sourceRenderer.sharedMaterials;
                renderer.enabled = sourceRenderer.enabled;
                renderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
                renderer.receiveShadows = sourceRenderer.receiveShadows;
                renderer.renderingLayerMask = sourceRenderer.renderingLayerMask;
            }
            foreach (Transform child in original)
                if (child.name != OwnedRoot &&
                    !child.GetComponent<SurfaceIvyInstance>() &&
                    !child.GetComponentInParent<SurfaceIvyInstance>())
                    CloneVisual(child, copy.transform);
            return copy.transform;
        }

        static Material[] PersistentMaterials(Material[] materials, string root, string key, RolloutResult totals)
        {
            var saved = (Material[])materials.Clone();
            for (int i = 0; i < saved.Length; ++i)
            {
                if (!saved[i] || EditorUtility.IsPersistent(saved[i])) continue;
                string path = root + "/Materials/G_" + Digest(key + "|" + i) + ".mat";
                var current = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!current)
                {
                    current = new Material(saved[i]) { name = saved[i].name };
                    AssetDatabase.CreateAsset(current, path);
                    AssetDatabase.SaveAssetIfDirty(current);
                    totals.generatedMaterials++;
                }
                saved[i] = current;
            }
            return saved;
        }

        static Mesh SaveMesh(Mesh source, string root, string key, RolloutResult totals)
        {
            if (!source) throw new ArgumentNullException(nameof(source));
            string path = root + "/Meshes/M_" + Digest(key) + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing)
            {
                EditorUtility.CopySerialized(source, existing);
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssetIfDirty(existing);
                return existing;
            }
            var mesh = Object.Instantiate(source);
            mesh.name = "A33_" + source.name;
            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssetIfDirty(mesh);
            totals.generatedMeshes++;
            return mesh;
        }

        static StageARuinRolloutBaseline LoadBaseline(string root)
        {
            string path = root + "/A33Baseline.asset";
            var asset = AssetDatabase.LoadAssetAtPath<StageARuinRolloutBaseline>(path);
            if (asset) return asset;
            asset = ScriptableObject.CreateInstance<StageARuinRolloutBaseline>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssetIfDirty(asset);
            return asset;
        }

        static void Restore(Row row, StageARuinRolloutBaselineRow baseline)
        {
            if (!row.renderer || !row.filter) return;
            var old = row.renderer.transform.Find(OwnedRoot);
            if (old) Object.DestroyImmediate(old.gameObject);
            row.filter.sharedMesh = baseline.sourceMesh;
            row.renderer.sharedMaterials = baseline.sourceMaterials;
            row.renderer.enabled = baseline.sourceEnabled;
            var binding = row.renderer.GetComponent<AMapRuinWeatheringBinding>();
            if (binding) Object.DestroyImmediate(binding);
            if (!baseline.sourcePropertyBlockOccupied) row.renderer.SetPropertyBlock(null);
        }

        static bool Protected(PolicyEntry entry)
        {
            return entry == null || Mode(entry.assetClass, "Protected") ||
                (Mode(entry.weatheringMode, "Preserve") &&
                 !Mode(entry.ivyMode, "Apply") &&
                 !Mode(entry.damageMode, "Apply") && !Mode(entry.damageMode, "SafeChip"));
        }

        static string Reason(PolicyEntry entry) => entry == null ? "missing policy" :
            entry.reasons == null ? "policy-protected" : string.Join(";", entry.reasons);
        static bool OwnedAncestor(Transform target, Transform map)
        {
            for (Transform t = target; t && t != map; t = t.parent)
                if (t.name == OwnedRoot) return true;
            return false;
        }
        static bool ActiveRelative(Transform item, Transform root)
        {
            for (var t=item; t && t!=root; t=t.parent)
                if (!t.gameObject.activeSelf) return false;
            return true;
        }
        static bool Mode(string value, string expected) =>
            string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
        static RuinAssetKind Kind(string value) => Mode(value, "Building") ? RuinAssetKind.Building :
            Mode(value, "Vehicle") ? RuinAssetKind.Vehicle : RuinAssetKind.Prop;

        public static string Key(Transform target, Transform map)
        {
            var names = new List<string>();
            for (Transform t = target; t && t != map; t = t.parent)
                names.Add(t.name + "[" + t.GetSiblingIndex() + "]");
            names.Reverse();
            return string.Join("/", names);
        }

        static int StableSeed(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            using (var hash = SHA256.Create())
            {
                var digest = hash.ComputeHash(bytes);
                int result = BitConverter.ToInt32(digest, 0);
                return result == 0 ? 731 : result;
            }
        }

        static string Digest(string value)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value)), 0, 12)
                    .Replace("-", "").ToLowerInvariant();
        }

        static string AssetRoot(string path)
        {
            path = (path ?? "").Replace('\\', '/').TrimEnd('/');
            if (path != DefaultAssetRoot &&
                !path.StartsWith(DefaultAssetRoot + "/", StringComparison.Ordinal))
                throw new InvalidOperationException("A33 may write only StageARollout-generated assets");
            return path;
        }

        static void EnsureFolders(string root)
        {
            string[] names = root.Split('/');
            string parent = names[0];
            for (int i = 1; i < names.Length; i++)
            {
                string path = parent + "/" + names[i];
                if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, names[i]);
                parent = path;
            }
            if (!AssetDatabase.IsValidFolder(root + "/Materials")) AssetDatabase.CreateFolder(root, "Materials");
            if (!AssetDatabase.IsValidFolder(root + "/Meshes")) AssetDatabase.CreateFolder(root, "Meshes");
        }
    }
}
#endif
