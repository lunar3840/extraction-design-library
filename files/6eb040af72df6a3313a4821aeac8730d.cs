using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Escape.EditorTools.Ivy;
using Escape.Stage.Blockout;
using Escape.SurfaceIvy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Escape.EditorTools
{
    /// <summary>
    /// One north CLINIC Game-camera art trial. No work is done on import or domain reload.
    /// The integrator must capture a fresh before Game view and hand off the editor first.
    /// The only scene addition is CLINIC.VisualRoot/__A34_ClinicMoodTrial.
    /// </summary>
    public static class StageAClinicMoodTrialBuilder
    {
        public const string RootName = "__A34_ClinicMoodTrial";
        public const string AssetRoot = "Assets/Escape/Art/ClinicMoodTrial20261030";
        const string Record = @"C:\Users\ehoji\OneDrive\문서\ChatGPT\익스트랙션\outputs\change_records\2026-10-30\a-map-clinic-reclaimed-mood-trial";
        const string FacadePath = "02_Visual_Replaceable/CLINIC__Visual/BlenderExpansion_CLINIC_Shell/CLINIC_Shell/CLINIC_Shell_renderOpaque";
        const string ExistingLeaf = "Assets/Escape/Art/SurfaceRuin/StageALargeIvy20261030/Materials/IvyLeaf.mat";
        const string ExistingStem = "Assets/Escape/Art/SurfaceRuin/StageALargeIvy20261030/Materials/IvyStem.mat";
        const string ExistingRubble = "Assets/Escape/Art/LandscapeV31/V31_FracturedRubble.mat";
        const string ExistingPlaster = "Assets/Escape/Art/BuildingD09/Materials/D09_PlasterIvory.mat";
        const string ExistingStone = "Assets/Escape/Art/BuildingD09/Materials/D09_StoneBase.mat";
        const string ExistingPuddle = "Assets/Escape/Art/WeatherV38/Puddles/V38_Puddles.mat";
        const string VersionMarker = "A34 candidate02 authored cluster";
        const string LargeIvyPolicy = @"C:\Users\ehoji\OneDrive\문서\ChatGPT\익스트랙션\outputs\change_records\2026-10-30\a-map-large-building-ivy\staging\profiles\ACTUAL_RENDERER_POLICY_LARGE_IVY.json";

        static readonly string[] MaterialNames = { "A34_Soil", "A34_Paving", "A34_DryLeaf", "A34_Wet", "A34_PlasterChip", "A34_Root", "A34_IvyLeaf", "A34_Substrate" };
        static readonly string[] MeshNames = { "A34_Ground", "A34_Debris", "A34_Roots", "A34_GrowthStem", "A34_GrowthLeaf", "A34_PlasterLoss" };

        sealed class Context
        {
            public StageABlockoutMap map;
            public StageABlockoutSemanticAnchor clinic;
            public MeshRenderer shell;
            public float wallZ;
            public float xMin, xMax;
            public List<Face> wall = new List<Face>();
            public List<Face> glass = new List<Face>();
            public StageARuinRolloutBuilder.WorldExclusion[] protectedOpenings;
        }

        struct Face
        {
            public Vector3 a, b, c;
            public float minX, maxX, minY, maxY;
            public bool Contains(float x, float y, float margin = 0f) =>
                x >= minX - margin && x <= maxX + margin && y >= minY - margin && y <= maxY + margin;
        }

        sealed class MeshBook
        {
            readonly List<Vector3> vertices = new List<Vector3>();
            readonly List<Vector2> uv = new List<Vector2>();
            readonly List<Vector2> radial = new List<Vector2>();
            readonly List<Color> colors = new List<Color>();
            readonly List<List<int>> triangles;
            public MeshBook(int materials)
            {
                triangles = Enumerable.Range(0, materials).Select(_ => new List<int>()).ToList();
            }
            public int QuadCount { get; private set; }
            public void Quad(int slot, Vector3 a, Vector3 b, Vector3 c, Vector3 d,
                bool twoSided = false, bool flipFront = false)
            {
                if (slot < 0 || slot >= triangles.Count) throw new ArgumentOutOfRangeException(nameof(slot));
                if (flipFront) { Vector3 swap = b; b = d; d = swap; }
                int i = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0));
                uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
                for (int v = 0; v < 4; v++) { radial.Add(Vector2.zero); colors.Add(Color.white); }
                triangles[slot].AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
                if (twoSided)
                {
                    int back = vertices.Count;
                    vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
                    uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0));
                    uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
                    for (int v = 0; v < 4; v++) { radial.Add(Vector2.zero); colors.Add(Color.white); }
                    triangles[slot].AddRange(new[] { back + 2, back + 1, back,
                        back + 3, back + 2, back });
                }
                QuadCount++;
            }
            public void WetFan(Vector3 center, Vector3[] rim, float phase)
            {
                if (triangles.Count < 4 || rim == null || rim.Length < 6)
                    throw new InvalidOperationException("Wet fan requires the A34 ground submesh");
                int first = vertices.Count;
                vertices.Add(center); uv.Add(new Vector2(.5f, .5f));
                radial.Add(new Vector2(0f, phase));
                colors.Add(new Color(0f, .86f, 0f, 1f));
                for (int i = 0; i < rim.Length; i++)
                {
                    vertices.Add(rim[i]);
                    float angle = 2f * Mathf.PI * i / rim.Length;
                    uv.Add(new Vector2(.5f + Mathf.Cos(angle) * .5f,
                        .5f + Mathf.Sin(angle) * .5f));
                    radial.Add(new Vector2(1f, phase));
                    colors.Add(new Color(0f, .86f, 0f, 1f));
                    int next = (i + 1) % rim.Length;
                    // Clockwise in XZ yields an upward-facing front for Cull Back.
                    triangles[3].AddRange(new[] { first, first + 1 + next, first + 1 + i });
                }
                QuadCount += rim.Length;
            }
            public void GroundFan(int slot, Vector3 center, Vector3[] rim)
            {
                if (rim == null || rim.Length < 5 || slot < 0 || slot >= triangles.Count)
                    throw new InvalidOperationException("Ground fan requires five or more perimeter vertices");
                int first = vertices.Count;
                vertices.Add(center); uv.Add(new Vector2(center.x * .42f, center.z * .42f));
                radial.Add(Vector2.zero); colors.Add(Color.white);
                for (int i = 0; i < rim.Length; i++)
                {
                    vertices.Add(rim[i]); uv.Add(new Vector2(rim[i].x * .42f, rim[i].z * .42f));
                    radial.Add(Vector2.zero); colors.Add(Color.white);
                    int next = (i + 1) % rim.Length;
                    triangles[slot].AddRange(new[] { first, first + 1 + next, first + 1 + i });
                }
                QuadCount += rim.Length;
            }
            public void ProfileLeaf(int slot, Vector3 center, float size,
                float angle, float asymmetry, float curl, int variant)
            {
                if (slot < 0 || slot >= triangles.Count)
                    throw new ArgumentOutOfRangeException(nameof(slot));
                int first = vertices.Count;
                Vector3 side = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                Vector3 longAxis = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0f);
                for (int i = 0; i < SurfaceIvyLeafProfile.VertexCount; i++)
                {
                    Vector3 v = SurfaceIvyLeafProfile.Evaluate(i, variant, asymmetry, curl);
                    vertices.Add(center + side * (v.x * size) +
                        longAxis * ((v.y - .47f) * size) + Vector3.forward * (v.z * size));
                    uv.Add(SurfaceIvyLeafProfile.UV(i, variant));
                    radial.Add(Vector2.zero); colors.Add(Color.white);
                }
                for (int i = 0; i < SurfaceIvyLeafProfile.BoundaryCount; i++)
                {
                    triangles[slot].AddRange(new[] { first + i,
                        first + (i + 1) % SurfaceIvyLeafProfile.BoundaryCount,
                        first + SurfaceIvyLeafProfile.BoundaryCount });
                }
                QuadCount++;
            }
            public Mesh Make(string name, Matrix4x4 worldToLocal)
            {
                if (QuadCount == 0) throw new InvalidOperationException("Empty mesh: " + name);
                if (vertices.Count >= 65535) throw new InvalidOperationException("Art trial vertex budget exceeded");
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt16 };
                mesh.SetVertices(vertices.Select(worldToLocal.MultiplyPoint3x4).ToList());
                mesh.SetUVs(0, uv);
                mesh.SetUVs(1, radial);
                mesh.SetColors(colors);
                mesh.subMeshCount = triangles.Count;
                for (int i = 0; i < triangles.Count; i++) mesh.SetTriangles(triangles[i], i);
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
                if (mesh.normals.Any(n => n.sqrMagnitude < .5f))
                    throw new InvalidOperationException("Degenerate A34 mesh normals: " + name);
                return mesh;
            }
        }

        public static string Apply()
        {
            return ApplyInternal(false);
        }

        /// <summary>
        /// Replace only the reviewed A34 candidate01 root and exact owned assets.
        /// The integrator retains the candidate01 snapshot before invoking this.
        /// </summary>
        public static string ApplyCandidate02()
        {
            string snapshot = Record + "/candidates/candidate01/generated_asset_snapshot/Meshes/A34_GrowthLeaf.asset";
            if (!File.Exists(snapshot))
                throw new InvalidOperationException("Candidate01 asset snapshot missing; replacement is refused");
            return ApplyInternal(true);
        }

        static string ApplyInternal(bool replaceCandidate01)
        {
            Context c = Preflight(requireBefore: true);
            Transform existing = c.clinic.VisualRoot.Find(RootName);
            if (existing)
            {
                if (!ValidExisting(existing, c.shell))
                    throw new InvalidOperationException("Partial/foreign A34 root; inspect and Rollback first");
                if (!replaceCandidate01) return "NO_OP existing scoped CLINIC mood trial; use Audit";
                var marker = existing.GetComponentInChildren<SurfaceIvyInstance>(true);
                if (marker && marker.generationReport == VersionMarker)
                {
                    string current = Audit();
                    if (!current.Contains("RESULT PASS"))
                        throw new InvalidOperationException("Candidate02 root is incomplete: " + current);
                    return "NO_OP candidate02 already applied; " + current;
                }
            }
            else if (replaceCandidate01)
            {
                throw new InvalidOperationException("Candidate01 root missing; use Apply for a clean A34 scene");
            }
            if (!replaceCandidate01)
            {
                foreach (string path in AssetPaths())
                    if (AssetDatabase.LoadMainAssetAtPath(path))
                        throw new InvalidOperationException("Owned asset exists without owned scene root: " + path);
            }
            else if (AssetPaths().Any(path => !AssetDatabase.LoadMainAssetAtPath(path)))
            {
                throw new InvalidOperationException("Candidate01 owned asset set is incomplete; replacement refused");
            }

            // Build and ground-survey all geometry before creating any Unity assets or scene nodes.
            var ground = new MeshBook(4); var debris = new MeshBook(1);
            var roots = new MeshBook(1); var stems = new MeshBook(1);
            var leaves = new MeshBook(1); var loss = new MeshBook(1);
            Compose(c, ground, debris, roots, stems, leaves, loss);
            if (ground.QuadCount > 190 || debris.QuadCount > 55 || roots.QuadCount > 85 ||
                stems.QuadCount > 110 || leaves.QuadCount > 170 || loss.QuadCount > 5)
                throw new InvalidOperationException("A34 geometry budget exceeded");

            GameObject owned = null;
            bool mutationStarted = false;
            try
            {
                if (replaceCandidate01)
                {
                    // This is the last operation before destructive replacement.
                    // The validator re-hashes the current post-UI checkpoint,
                    // retained sources, owned 14 assets/root and approved before
                    // evidence exactly once in this main call. Any failed/stale
                    // gate leaves the scene and AssetDatabase untouched.
                    string gate = global::Escape.EditorTools.ClinicMoodTrial
                        .StageAClinicMoodTrialValidation.ValidatePostUiBeforeCandidate02();
                    if (string.IsNullOrEmpty(gate) ||
                        !gate.StartsWith("PASS_POST_UI_BEFORE_GATE", StringComparison.Ordinal))
                        throw new InvalidOperationException("Candidate02 post-UI before gate failed: " + gate);
                    mutationStarted = true;
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
                    DeleteOwnedAssets();
                }
                else mutationStarted = true;
                EnsureFolder(AssetRoot + "/Materials"); EnsureFolder(AssetRoot + "/Meshes");
                Material[] materials = CreateMaterials();
                owned = new GameObject(RootName);
                owned.transform.SetParent(c.clinic.VisualRoot, false);
                owned.layer = c.shell.gameObject.layer;
                var frame = owned.transform.worldToLocalMatrix;
                Transform groundRoot = Child(owned.transform, "GroundAndDeposits");
                Transform facadeRoot = Child(owned.transform, "GrowthCanopy");
                CreateMesh(groundRoot, "A34_Ground", ground, frame,
                    new[] { materials[0], materials[1], materials[2], materials[3] });
                CreateMesh(groundRoot, "A34_Debris", debris, frame, new[] { materials[4] });
                CreateMesh(groundRoot, "A34_Roots", roots, frame, new[] { materials[5] });
                CreateMesh(facadeRoot, "A34_GrowthStem", stems, frame, new[] { materials[5] });
                CreateMesh(facadeRoot, "A34_GrowthLeaf", leaves, frame, new[] { materials[6] });
                CreateMesh(facadeRoot, "A34_PlasterLoss", loss, frame, new[] { materials[7] });

                var marker = facadeRoot.gameObject.AddComponent<SurfaceIvyInstance>();
                marker.sourceTarget = c.shell.transform;
                marker.sourceRenderers = new Renderer[] { c.shell };
                marker.followSourceVisibility = true;
                marker.isPreview = false;
                marker.generationReport = VersionMarker;
                facadeRoot.gameObject.AddComponent<AMapRuinIvyRevealBinding>().Configure(marker);
                EditorSceneManager.MarkSceneDirty(c.map.gameObject.scene);
                AssetDatabase.SaveAssets();
                string audit = Audit();
                if (!audit.Contains("RESULT PASS")) throw new InvalidOperationException(audit);
                return (replaceCandidate01 ? "CANDIDATE02_REPLACED " : "APPLIED ") + audit;
            }
            catch
            {
                if (mutationStarted)
                {
                    if (owned) UnityEngine.Object.DestroyImmediate(owned);
                    DeleteOwnedAssets();
                }
                throw;
            }
        }

        public static string Rollback()
        {
            Context c = Preflight(requireBefore: false);
            Transform owned = c.clinic.VisualRoot.Find(RootName);
            if (owned) UnityEngine.Object.DestroyImmediate(owned.gameObject);
            DeleteOwnedAssets();
            EditorSceneManager.MarkSceneDirty(c.map.gameObject.scene);
            return "ROLLBACK scoped root and exact owned assets removed; scene/prefab save is integrator-owned";
        }

        public static string Audit()
        {
            Context c = Preflight(requireBefore: false);
            Transform root = c.clinic.VisualRoot.Find(RootName);
            var lines = new List<string> { "A34_CLINIC_MOOD_TRIAL_AUDIT" };
            bool pass = root && ValidExisting(root, c.shell);
            lines.Add("root=" + (root ? root.name : "MISSING"));
            if (root)
            {
                var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
                int vertices = renderers.Sum(r => r.GetComponent<MeshFilter>()?.sharedMesh?.vertexCount ?? 0);
                int triangles = renderers.Sum(r =>
                {
                    var mesh = r.GetComponent<MeshFilter>()?.sharedMesh;
                    if (!mesh) return 0;
                    int count = 0;
                    for (int s = 0; s < mesh.subMeshCount; s++) count += (int)mesh.GetIndexCount(s) / 3;
                    return count;
                });
                int colliders = root.GetComponentsInChildren<Collider>(true).Length;
                int lights = root.GetComponentsInChildren<Light>(true).Length;
                lines.Add("renderers=" + renderers.Length + " vertices=" + vertices + " triangles=" + triangles +
                    " colliders=" + colliders + " lights=" + lights);
                pass &= renderers.Length == 6 && vertices <= 7000 && triangles <= 5000 && colliders == 0 && lights == 0;
                foreach (var r in renderers)
                    pass &= r.sharedMaterials.All(m => m && AssetDatabase.GetAssetPath(m).StartsWith(AssetRoot + "/", StringComparison.Ordinal));
                bool v33DirectNames = renderers.Any(r => new[] { "Roof", "Facade", "Wall", "Ceiling",
                    "Cladding", "Eave", "Awning" }.Any(token =>
                    r.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0));
                lines.Add("v33DirectNameEligible=" + v33DirectNames);
                pass &= !v33DirectNames;
                var groundMesh = root.Find("GroundAndDeposits/A34_Ground")?.GetComponent<MeshFilter>()?.sharedMesh;
                Material wetMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                    AssetRoot + "/Materials/A34_Wet.mat");
                bool wetChannels = groundMesh && wetMaterial && wetMaterial.shader &&
                    wetMaterial.shader.name == "Escape/StageA/PuddlesV38" &&
                    wetMaterial.HasProperty("_WetOpacity") && wetMaterial.GetFloat("_WetOpacity") > .1f &&
                    WetChannelsValid(groundMesh);
                lines.Add("wetRadialAndColorChannels=" + wetChannels);
                pass &= wetChannels;
                var leafMesh = root.Find("GrowthCanopy/A34_GrowthLeaf")?.GetComponent<MeshFilter>()?.sharedMesh;
                Material leafMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                    AssetRoot + "/Materials/A34_IvyLeaf.mat");
                Material originalLeaf = AssetDatabase.LoadAssetAtPath<Material>(ExistingLeaf);
                int leafCount = leafMesh ? leafMesh.vertexCount / SurfaceIvyLeafProfile.VertexCount : 0;
                bool profileLeaves = leafMesh && leafMesh.vertexCount % SurfaceIvyLeafProfile.VertexCount == 0 &&
                    (int)leafMesh.GetIndexCount(0) == leafCount * SurfaceIvyLeafProfile.BoundaryCount * 3 &&
                    leafCount >= 100 && leafCount <= 160;
                bool originalLeafTint = leafMaterial && originalLeaf &&
                    leafMaterial.HasProperty("_BaseColor") && originalLeaf.HasProperty("_BaseColor") &&
                    leafMaterial.GetColor("_BaseColor") == originalLeaf.GetColor("_BaseColor") &&
                    leafMaterial.mainTexture == originalLeaf.mainTexture;
                lines.Add("profileLeaves=" + leafCount + " valid=" + profileLeaves +
                    " originalLeafTintAndTexture=" + originalLeafTint);
                pass &= profileLeaves && originalLeafTint;
            }
            lines.Add("RESULT " + (pass ? "PASS" : "FAIL"));
            return string.Join("\n", lines) + "\n";
        }

        static bool WetChannelsValid(Mesh mesh)
        {
            if (mesh.subMeshCount < 4 ||
                !mesh.HasVertexAttribute(VertexAttribute.TexCoord1) ||
                !mesh.HasVertexAttribute(VertexAttribute.Color)) return false;
            var uv1 = new List<Vector2>();
            mesh.GetUVs(1, uv1);
            Color[] color = mesh.colors;
            if (uv1.Count != mesh.vertexCount || color.Length != mesh.vertexCount) return false;
            int[] wet = mesh.GetTriangles(3);
            if (wet.Length != 3 * 8 * 3) return false;
            var centers = new HashSet<int>();
            for (int i = 0; i + 2 < wet.Length; i += 3)
            {
                int center = wet[i], next = wet[i + 1], rim = wet[i + 2];
                centers.Add(center);
                if (uv1[center].x > .01f || uv1[next].x < .99f || uv1[rim].x < .99f ||
                    color[center].r > .001f || color[next].r > .001f || color[rim].r > .001f ||
                    color[center].g < .8f || color[next].g < .8f || color[rim].g < .8f ||
                    Mathf.Abs(uv1[center].y - uv1[next].y) > .001f ||
                    Mathf.Abs(uv1[center].y - uv1[rim].y) > .001f)
                    return false;
            }
            return centers.Count == 3;
        }

        /// <summary>Read-only site diagnosis before capturing or applying the trial.</summary>
        public static string Probe()
        {
            Context c = Preflight(requireBefore: false);
            var report = new List<string> {
                "A34_CLINIC_NORTH_PROBE",
                "scene=" + c.map.gameObject.scene.path + " dirty=" + c.map.gameObject.scene.isDirty,
                "source=" + FacadePath,
                "shellBounds=" + c.shell.bounds,
                "northOpaqueFaces=" + c.wall.Count + " northGlassFaces=" + c.glass.Count,
                "protectedDoorWindowBoxes=" + c.protectedOpenings.Length,
                "referenceGamePlayer=(-166,4.6381,155) camera=(-158,17,167) groundY=4.5981"
            };
            foreach (float x in new[] { -173.5f, -168.35f, -163.2f })
            {
                Vector3 wall = RawFacade(c, x, 4.83f, out Vector3 p) ? p :
                    RawFacade(c, x, 8.82f, out p) ? p : Vector3.zero;
                Vector3 ground = Ground(c, x, 1.2f);
                report.Add("x=" + x.ToString("F2") + " wall=" + wall.ToString("F3") +
                    " ground=" + ground.ToString("F3"));
            }
            return string.Join("\n", report) + "\n";
        }

        /// <summary>
        /// Diagnostic only. Enumerates both scene siblings and the prefab asset;
        /// never creates, reverts, saves or changes any Unity object or asset.
        /// The returned report is also archived under this task's staging/builder.
        /// </summary>
        public static string DiagnoseOwnedRoots()
        {
            var map = UnityEngine.Object.FindFirstObjectByType<StageABlockoutMap>();
            if (!map || map.gameObject.scene.path != StageV08Build.Scene)
                throw new InvalidOperationException("Open retained Stage A scene for A34 root diagnosis");
            var structure = map.GetComponent<StageABlockoutStructureV08>();
            var clinic = structure ? structure.FindAnchor("CLINIC") : null;
            if (!clinic || !clinic.VisualRoot)
                throw new InvalidOperationException("CLINIC visual root missing for A34 root diagnosis");
            var report = new StringBuilder("A34_OWNED_ROOT_DIAGNOSTIC_ONLY\n");
            var cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            report.AppendLine("scene=" + map.gameObject.scene.path +
                " dirty=" + map.gameObject.scene.isDirty +
                " sceneSha256=" + DiagnosticHashFile(map.gameObject.scene.path, cache));
            report.AppendLine("prefab=" + StageV08Build.Prefab +
                " prefabSha256=" + DiagnosticHashFile(StageV08Build.Prefab, cache));
            report.AppendLine("mapPartOfPrefabInstance=" + PrefabUtility.IsPartOfPrefabInstance(map.gameObject) +
                " mapPrefabStatus=" + PrefabUtility.GetPrefabInstanceStatus(map.gameObject));
            report.AppendLine("clinicVisualRoot=" + DiagnosticPath(clinic.VisualRoot, map.transform));
            Transform expectedSource = map.transform.Find(FacadePath);
            Renderer expectedRenderer = expectedSource ? expectedSource.GetComponent<MeshRenderer>() : null;
            report.AppendLine("expectedSourceRenderer=" + (expectedRenderer ?
                DiagnosticPath(expectedRenderer.transform, map.transform) : "MISSING"));

            Transform[] sceneRoots = DirectOwnedRoots(clinic.VisualRoot);
            report.AppendLine("sceneDirectNamedRootCount=" + sceneRoots.Length);
            foreach (Transform root in sceneRoots)
                AppendOwnedRoot(report, root, map.transform, expectedRenderer, cache, "scene");
            string[] sceneSignatures = sceneRoots.Select(root =>
                DiagnosticVisualSignature(root, expectedRenderer, cache)).ToArray();
            for (int i = 0; i < sceneSignatures.Length; i++)
                report.AppendLine("sceneRootVisualSignature[" + i + "]=" + sceneSignatures[i]);
            if (sceneSignatures.Length == 2)
                report.AppendLine("sceneTwoRootsVisualSignatureEqual=" +
                    string.Equals(sceneSignatures[0], sceneSignatures[1], StringComparison.Ordinal));

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StageV08Build.Prefab);
            Transform prefabClinic = prefab ?
                prefab.transform.Find("02_Visual_Replaceable/CLINIC__Visual") : null;
            Transform[] prefabRoots = prefabClinic ? DirectOwnedRoots(prefabClinic) : Array.Empty<Transform>();
            report.AppendLine("prefabSourceClinicFound=" + (prefabClinic != null) +
                " prefabDirectNamedRootCount=" + prefabRoots.Length);
            foreach (Transform root in prefabRoots)
                AppendOwnedRoot(report, root, prefab.transform, null, cache, "prefabAsset");

            AppendAddedOverrides(report, map.gameObject, map.transform);
            report.AppendLine("HYPOTHESIS_ONLY prefabSourcePlusSceneOverride=" +
                (sceneRoots.Length == 2 && prefabRoots.Length == 1 &&
                 sceneRoots.Count(r => PrefabUtility.IsAddedGameObjectOverride(r.gameObject)) == 1));
            report.AppendLine("RESULT DIAGNOSTIC_ONLY_NO_UNITY_MUTATION");
            string body = report.ToString();
            string folder = Record + "/staging/builder";
            Directory.CreateDirectory(folder);
            string path = folder + "/OWNED_ROOT_DIAGNOSTIC_" +
                DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ") + ".txt";
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, Encoding.UTF8)) writer.Write(body);
            return body + "reportFile=" + path + "\n";
        }

        static Transform[] DirectOwnedRoots(Transform parent) =>
            parent.Cast<Transform>().Where(t => t.name == RootName).ToArray();

        static void AppendOwnedRoot(StringBuilder report, Transform root, Transform ancestor,
            Renderer expectedRenderer, Dictionary<string, string> cache, string kind)
        {
            var go = root.gameObject;
            UnityEngine.Object source = PrefabUtility.GetCorrespondingObjectFromSource(go);
            string sourceGuid = "none"; long sourceLocalId = 0;
            if (source) AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out sourceGuid, out sourceLocalId);
            report.AppendLine(kind + "Root path=" + DiagnosticPath(root, ancestor) +
                " siblingIndex=" + root.GetSiblingIndex() +
                " active=" + go.activeSelf +
                " directChildren=" + root.childCount +
                " transforms=" + root.GetComponentsInChildren<Transform>(true).Length +
                " renderers=" + root.GetComponentsInChildren<MeshRenderer>(true).Length +
                " colliders=" + root.GetComponentsInChildren<Collider>(true).Length +
                " isAddedGameObjectOverride=" + PrefabUtility.IsAddedGameObjectOverride(go) +
                " prefabStatus=" + PrefabUtility.GetPrefabInstanceStatus(go) +
                " correspondingSourceGuid=" + sourceGuid +
                " correspondingSourceLocalId=" + sourceLocalId +
                " globalId=" + GlobalObjectId.GetGlobalObjectIdSlow(go));
            foreach (var marker in root.GetComponentsInChildren<SurfaceIvyInstance>(true))
            {
                bool targetMissing = !marker.sourceTarget;
                var refs = marker.sourceRenderers ?? Array.Empty<Renderer>();
                report.AppendLine("  ivyMarker=" + DiagnosticPath(marker.transform, ancestor) +
                    " sourceTargetMissing=" + targetMissing +
                    " sourceTarget=" + (targetMissing ? "MISSING" : DiagnosticPath(marker.sourceTarget, ancestor)) +
                    " sourceRenderers=" + refs.Length +
                    " nullRendererRefs=" + refs.Count(r => !r) +
                    " targetMatchesExpected=" + (expectedRenderer && marker.sourceTarget == expectedRenderer.transform) +
                    " rendererMatchesExpected=" + (expectedRenderer && refs.Length == 1 && refs[0] == expectedRenderer));
            }
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                report.AppendLine("  renderer=" + DiagnosticPath(renderer.transform, ancestor) +
                    " enabled=" + renderer.enabled +
                    " mesh=" + DiagnosticAsset(filter ? filter.sharedMesh : null, cache) +
                    " materials=" + string.Join(";", renderer.sharedMaterials.Select(m => DiagnosticAsset(m, cache))));
            }
        }

        static string DiagnosticAsset(UnityEngine.Object asset, Dictionary<string, string> cache)
        {
            if (!asset) return "MISSING";
            string path = AssetDatabase.GetAssetPath(asset);
            string guid = string.Empty; long localId = 0;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out guid, out localId);
            return asset.name + "@" + path + " guid=" + guid + " localId=" + localId +
                " sha256=" + DiagnosticHashFile(path, cache);
        }

        static string DiagnosticVisualSignature(Transform root, Renderer expectedRenderer,
            Dictionary<string, string> cache)
        {
            var raw = new StringBuilder();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)
                .OrderBy(t => DiagnosticPath(t, root), StringComparer.Ordinal))
            {
                raw.Append(DiagnosticPath(t, root)).Append('|')
                    .Append(t.gameObject.activeSelf).Append('|')
                    .Append(t.gameObject.layer).Append('|')
                    .Append(t.gameObject.tag).Append('|')
                    .Append(DiagnosticVector(t.localPosition)).Append('|')
                    .Append(DiagnosticQuaternion(t.localRotation)).Append('|')
                    .Append(DiagnosticVector(t.localScale));
                MeshFilter filter = t.GetComponent<MeshFilter>();
                if (filter) raw.Append("|mesh=").Append(DiagnosticAsset(filter.sharedMesh, cache));
                MeshRenderer renderer = t.GetComponent<MeshRenderer>();
                if (renderer)
                    raw.Append("|renderer=").Append(renderer.enabled)
                        .Append(':').Append(renderer.shadowCastingMode)
                        .Append(':').Append(renderer.receiveShadows)
                        .Append(':').Append(string.Join(";", renderer.sharedMaterials
                            .Select(m => DiagnosticAsset(m, cache))));
                SurfaceIvyInstance marker = t.GetComponent<SurfaceIvyInstance>();
                if (marker)
                {
                    Renderer[] refs = marker.sourceRenderers ?? Array.Empty<Renderer>();
                    raw.Append("|ivy=").Append(marker.followSourceVisibility)
                        .Append(':').Append(marker.isPreview)
                        .Append(':').Append(marker.generationReport)
                        .Append(':').Append(expectedRenderer && marker.sourceTarget == expectedRenderer.transform)
                        .Append(':').Append(refs.Length)
                        .Append(':').Append(refs.Count(r => !r))
                        .Append(':').Append(expectedRenderer && refs.Length == 1 && refs[0] == expectedRenderer);
                }
                raw.Append("|binding=").Append(t.GetComponent<AMapRuinIvyRevealBinding>() != null)
                    .Append('\n');
            }
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw.ToString()))).Replace("-", "");
        }

        static string DiagnosticVector(Vector3 v) =>
            v.x.ToString("R", CultureInfo.InvariantCulture) + "," +
            v.y.ToString("R", CultureInfo.InvariantCulture) + "," +
            v.z.ToString("R", CultureInfo.InvariantCulture);

        static string DiagnosticQuaternion(Quaternion q) =>
            q.x.ToString("R", CultureInfo.InvariantCulture) + "," +
            q.y.ToString("R", CultureInfo.InvariantCulture) + "," +
            q.z.ToString("R", CultureInfo.InvariantCulture) + "," +
            q.w.ToString("R", CultureInfo.InvariantCulture);

        static string DiagnosticHashFile(string path, Dictionary<string, string> cache)
        {
            if (string.IsNullOrEmpty(path)) return "NO_PATH";
            if (cache.TryGetValue(path, out string known)) return known;
            string absolute = Path.GetFullPath(path);
            if (!File.Exists(absolute)) return cache[path] = "MISSING_FILE";
            using (var stream = File.OpenRead(absolute))
            using (var sha = SHA256.Create())
                return cache[path] = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
        }

        static string DiagnosticPath(Transform target, Transform ancestor)
        {
            if (!target) return "MISSING";
            var parts = new List<string>();
            for (Transform t = target; t; t = t.parent)
            {
                int sameNameBefore = 0;
                if (t.parent)
                    for (int i = 0; i < t.GetSiblingIndex(); i++)
                        if (t.parent.GetChild(i).name == t.name) sameNameBefore++;
                parts.Add(t.name + "#" + sameNameBefore);
                if (t == ancestor) break;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        static void AppendAddedOverrides(StringBuilder report, GameObject map, Transform ancestor)
        {
            try
            {
                MethodInfo method = typeof(PrefabUtility).GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "GetAddedGameObjects" &&
                        m.GetParameters().Length == 1 &&
                        m.GetParameters()[0].ParameterType == typeof(GameObject));
                if (method == null) { report.AppendLine("addedOverrideList=API_UNAVAILABLE"); return; }
                IEnumerable items = method.Invoke(null, new object[] { map }) as IEnumerable;
                if (items == null) { report.AppendLine("addedOverrideList=NULL"); return; }
                int count = 0;
                foreach (object item in items)
                {
                    count++;
                    Type type = item.GetType();
                    var field = type.GetField("instanceGameObject", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var property = type.GetProperty("instanceGameObject", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    GameObject gameObject = (field?.GetValue(item) ?? property?.GetValue(item)) as GameObject;
                    report.AppendLine("addedOverride[" + (count - 1) + "]=" +
                        (gameObject ? DiagnosticPath(gameObject.transform, ancestor) : type.FullName + ":UNREADABLE"));
                }
                report.AppendLine("addedOverrideCount=" + count);
            }
            catch (Exception error) { report.AppendLine("addedOverrideList=ERROR " + error); }
        }

        static Context Preflight(bool requireBefore)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("A34 authoring requires Edit mode");
            var map = StageAGraphicsV31Workflow.Map();
            if (!map || map.gameObject.scene.path != StageV08Build.Scene)
                throw new InvalidOperationException("Open retained Stage A scene");
            var structure = map.GetComponent<StageABlockoutStructureV08>();
            var clinic = structure ? structure.FindAnchor("CLINIC") : null;
            if (!clinic || !clinic.VisualRoot) throw new InvalidOperationException("CLINIC semantic visual root missing");
            Transform source = map.transform.Find(FacadePath);
            if (!source || !source.IsChildOf(clinic.VisualRoot))
                throw new InvalidOperationException("Retained CLINIC north facade source changed");
            var shell = source.GetComponent<MeshRenderer>();
            var filter = source.GetComponent<MeshFilter>();
            if (!shell || !filter || !filter.sharedMesh || !filter.sharedMesh.isReadable || !shell.enabled)
                throw new InvalidOperationException("Readable active CLINIC shell missing");
            if (requireBefore)
            {
                if (map.gameObject.scene.isDirty) throw new InvalidOperationException("Save clean Stage A baseline first");
                if (!Directory.Exists(Record) || Directory.GetFiles(Record, "before_*.png").Length == 0)
                    throw new InvalidOperationException("Fresh before Game screenshot required before A34 apply");
            }
            var c = new Context { map = map, clinic = clinic, shell = shell,
                xMin = -177f, xMax = -161f };
            if (!File.Exists(LargeIvyPolicy))
                throw new InvalidOperationException("Reviewed 34-opening CLINIC exclusion policy missing");
            var policy = JsonUtility.FromJson<StageARuinRolloutBuilder.PolicyFile>(File.ReadAllText(LargeIvyPolicy));
            var row = policy?.entries?.SingleOrDefault(e => e.ivyGroup == "building:CLINIC:facade");
            if (row?.ivyExclusionsWorld == null || row.ivyExclusionsWorld.Length != 34)
                throw new InvalidOperationException("CLINIC door/window exclusion mask changed");
            c.protectedOpenings = row.ivyExclusionsWorld;
            SurveyNorthFaces(c, filter);
            if (c.wall.Count < 4 || c.wallZ < 142f || c.wallZ > 150f)
                throw new InvalidOperationException("North CLINIC facade triangulation/bounds changed");
            return c;
        }

        static void SurveyNorthFaces(Context c, MeshFilter filter)
        {
            Mesh mesh = filter.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            Material[] materials = c.shell.sharedMaterials;
            Matrix4x4 world = filter.transform.localToWorldMatrix;
            float maxZ = c.shell.bounds.max.z;
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                int[] indices = mesh.GetTriangles(s);
                bool glass = s < materials.Length && materials[s] &&
                    materials[s].name.IndexOf("Glass", StringComparison.OrdinalIgnoreCase) >= 0;
                for (int i = 0; i + 2 < indices.Length; i += 3)
                {
                    Vector3 a = world.MultiplyPoint3x4(vertices[indices[i]]);
                    Vector3 b = world.MultiplyPoint3x4(vertices[indices[i + 1]]);
                    Vector3 d = world.MultiplyPoint3x4(vertices[indices[i + 2]]);
                    Vector3 normal = Vector3.Cross(b - a, d - a).normalized;
                    float z = (a.z + b.z + d.z) / 3f;
                    if (Mathf.Abs(normal.z) < .6f || z < maxZ - 12f ||
                        Mathf.Max(a.y, Mathf.Max(b.y, d.y)) < 4.7f ||
                        Mathf.Min(a.y, Mathf.Min(b.y, d.y)) > 9.7f) continue;
                    var face = new Face { a = a, b = b, c = d,
                        minX = Mathf.Min(a.x, Mathf.Min(b.x, d.x)),
                        maxX = Mathf.Max(a.x, Mathf.Max(b.x, d.x)),
                        minY = Mathf.Min(a.y, Mathf.Min(b.y, d.y)),
                        maxY = Mathf.Max(a.y, Mathf.Max(b.y, d.y)) };
                    (glass ? c.glass : c.wall).Add(face);
                }
            }
            c.wallZ = c.wall.Count == 0 ? 0f : c.wall.OrderByDescending(f => (f.a.z + f.b.z + f.c.z) / 3f)
                .Skip(Mathf.Min(10, c.wall.Count - 1))
                .Select(f => (f.a.z + f.b.z + f.c.z) / 3f).First();
        }

        // True mesh intersection on the north-facing facade. Never use the FBX local-100 scale as metres.
        static bool Facade(Context c, float x, float y, out Vector3 point)
        {
            point = default;
            // The prior 34-box survey covers separate door/window renderers too.
            // Margin includes the rotated profile leaf extent plus framing gap.
            foreach (var opening in c.protectedOpenings)
                if (Mathf.Abs(x - opening.center.x) <= opening.size.x * .5f + .40f &&
                    Mathf.Abs(y - opening.center.y) <= opening.size.y * .5f + .40f &&
                    Mathf.Abs(c.wallZ - opening.center.z) <= 13f) return false;
            foreach (Face glass in c.glass)
                if (glass.Contains(x, y, .23f)) return false;
            return RawFacade(c, x, y, out point);
        }

        // Raw wall profile is used only to ground the separate floor dressing.
        // Door/window exclusion still gates every facade leaf, stem and chip.
        static bool RawFacade(Context c, float x, float y, out Vector3 point)
        {
            point = default;
            float best = float.NegativeInfinity;
            foreach (Face f in c.wall)
            {
                if (!f.Contains(x, y) || !IntersectXY(f, x, y, out float z)) continue;
                if (z > best) best = z;
            }
            if (best < 140f) return false;
            point = new Vector3(x, y, best + .045f);
            return true;
        }

        static bool IntersectXY(Face f, float x, float y, out float z)
        {
            float den = (f.b.y - f.c.y) * (f.a.x - f.c.x) +
                (f.c.x - f.b.x) * (f.a.y - f.c.y);
            z = 0f;
            if (Mathf.Abs(den) < .00001f) return false;
            float u = ((f.b.y - f.c.y) * (x - f.c.x) +
                (f.c.x - f.b.x) * (y - f.c.y)) / den;
            float v = ((f.c.y - f.a.y) * (x - f.c.x) +
                (f.a.x - f.c.x) * (y - f.c.y)) / den;
            if (u < -.001f || v < -.001f || u + v > 1.001f) return false;
            z = u * f.a.z + v * f.b.z + (1f - u - v) * f.c.z;
            return true;
        }

        static float FrontZ(Context c, float x)
        {
            if (RawFacade(c, x, 4.83f, out Vector3 low)) return low.z - .045f;
            if (RawFacade(c, x, 8.82f, out Vector3 high)) return high.z - .045f;
            throw new InvalidOperationException("No CLINIC north wall profile at x=" + x);
        }

        static Vector3 Ground(Context c, float x, float outwardMetres, float offset = .022f)
        {
            if (outwardMetres < .25f) throw new InvalidOperationException("Ground decoration crossed CLINIC wall");
            float z = FrontZ(c, x) + outwardMetres;
            RaycastHit[] hits = Physics.RaycastAll(new Ray(new Vector3(x, 15f, z), Vector3.down), 17f);
            var valid = hits.Where(h => h.collider && h.normal.y > .72f &&
                h.point.y >= 3.4f && h.point.y <= 5.6f &&
                !h.collider.transform.IsChildOf(c.clinic.VisualRoot))
                .OrderByDescending(h => h.point.y).ToArray();
            if (valid.Length == 0) throw new InvalidOperationException("No ground at " + x + "," + z);
            return valid[0].point + Vector3.up * offset;
        }

        static void Compose(Context c, MeshBook ground, MeshBook debris, MeshBook roots,
            MeshBook stems, MeshBook leaves, MeshBook loss)
        {
            // The selected x interval sits left of the player's north Game view. It keeps the
            // centre route open and groups change around one wall-foot growth origin.
            var random = new System.Random(341031);
            // Three broken paving islands with ragged soil borders. All polygons hug
            // the retained ground and have no collision or walkable height.
            float[] islands = { -172.2f, -168.35f, -165.25f };
            for (int island = 0; island < islands.Length; island++)
            {
                float x = islands[island], d = island == 1 ? 1.30f : 1.12f;
                ground.GroundFan(0, Ground(c, x, d, .027f),
                    GroundRim(c, x, d, 1.20f, .70f, 9, island, .027f));
                ground.GroundFan(1, Ground(c, x + .12f, d - .08f, .039f),
                    GroundRim(c, x + .12f, d - .08f, .78f, .43f, 8, island + 7, .039f));
                var wet = GroundRim(c, x + .16f, d + .14f, .59f, .38f, 8,
                    island + 13, .052f);
                ground.WetFan(Ground(c, x + .16f, d + .14f, .052f), wet,
                    .13f + island * .21f);
            }
            // A few isolated cracks leave the original grass dominant between islands.
            for (int i = 0; i < 5; i++)
            {
                float x = islands[i % 3] + (i % 2 == 0 ? -.50f : .42f);
                float d = 1.4f + .09f * i;
                ground.Quad(0, Ground(c, x, d, .042f), Ground(c, x + .045f, d + .02f, .042f),
                    Ground(c, x + .16f, d + .72f, .042f),
                    Ground(c, x + .07f, d + .68f, .042f), flipFront: true);
            }
            for (int i = 0; i < 72; i++)
            {
                float center = islands[i % 3];
                float x = center + ((float)random.NextDouble() - .5f) * 2.75f;
                float d = .54f + (float)random.NextDouble() * 1.55f;
                Vector3 p = Ground(c, x, d, .058f);
                float sx = .10f + (float)random.NextDouble() * .13f;
                ground.Quad(2, p, Ground(c, x + sx, d + .035f, .058f),
                    Ground(c, x + sx * .52f, d + .13f, .058f),
                    Ground(c, x - .045f, d + .07f, .058f), true);
            }
            for (int i = 0; i < 30; i++)
            {
                float center = islands[i % 3];
                float x = center + ((float)random.NextDouble() - .5f) * 1.9f;
                float d = .58f + (float)random.NextDouble() * 1.15f;
                Vector3 p = Ground(c, x, d, .071f);
                float w = .14f + (float)random.NextDouble() * .24f;
                debris.Quad(0, p, p + new Vector3(w, .025f, 0),
                    p + new Vector3(w * .72f, .075f, w * .55f),
                    p + new Vector3(-.05f, .012f, w * .68f), flipFront: true);
            }

            // Two roots fan into the disturbed ground. Short curved ribbons taper
            // to thin tips; no long dark cable is left on the wall or path.
            float[] origins = { -168.76f, -167.95f };
            foreach (float ox in origins)
            {
                Vector3 foot = Ground(c, ox, .39f, .068f);
                if (!Facade(c, ox, 4.72f, out Vector3 wallFoot))
                    throw new InvalidOperationException("Wall-foot root connection missing at x=" + ox);
                Ribbon(roots, 0, foot, wallFoot, .032f, Vector3.right);
                for (int branch = -1; branch <= 1; branch++)
                {
                    Vector3 previous = foot;
                    for (int k = 1; k <= 6; k++)
                    {
                        float t = k / 6f;
                        float x = ox + branch * .55f * t + .08f * Mathf.Sin(t * 5f + branch);
                        Vector3 next = Ground(c, x, .39f + 1.60f * t,
                            .068f - .023f * t);
                        Ribbon(roots, 0, previous, next, Mathf.Lerp(.032f, .008f, t), Vector3.right);
                        previous = next;
                    }
                }
            }
            for (int trunk = 0; trunk < origins.Length; trunk++)
            {
                float ox = origins[trunk];
                Vector3 previous = default;
                bool previousValid = false;
                for (int k = 0; k <= 21; k++)
                {
                    float t = k / 21f, y = 4.76f + 4.25f * t;
                    float x = ox + .11f * Mathf.Sin(t * 9f + trunk * 1.5f);
                    if (!Facade(c, x, y, out Vector3 point))
                    {
                        previousValid = false; // never bridge a masked window/opening
                        continue;
                    }
                    if (previousValid && Vector3.Distance(previous, point) < .31f)
                        Ribbon(stems, 0, previous, point, Mathf.Lerp(.029f, .007f, t), Vector3.right);
                    previous = point;
                    previousValid = true;
                }
                for (int branch = 0; branch < 4; branch++)
                {
                    float startY = 5.35f + branch * .76f;
                    float sign = (branch + trunk) % 2 == 0 ? -1f : 1f;
                    previousValid = false;
                    for (int k = 0; k <= 5; k++)
                    {
                        float t = k / 5f;
                        float x = ox + sign * (.09f + .43f * t) + .035f * Mathf.Sin(t * 5.8f);
                        float y = startY + .79f * t;
                        if (!Facade(c, x, y, out Vector3 point))
                        {
                            previousValid = false;
                            continue;
                        }
                        if (previousValid && Vector3.Distance(previous, point) < .30f)
                            Ribbon(stems, 0, previous, point,
                                Mathf.Lerp(.018f, .006f, t), Vector3.right);
                        previous = point;
                        previousValid = true;
                    }
                }
            }

            // Existing three/five-lobed leaf geometry and UVs, scattered in two
            // uneven lobes with small lower sprigs to cover the stem junctions.
            int accepted = 0;
            for (int trunk = 0; trunk < origins.Length; trunk++)
            for (int sprig = 0; sprig < 5; sprig++)
            {
                float y = 5.16f + sprig * .30f;
                float x = origins[trunk] + .13f * Mathf.Sin(sprig * 1.7f + trunk);
                if (!Facade(c, x, y, out Vector3 point)) continue;
                point.z += .055f + sprig * .006f;
                leaves.ProfileLeaf(0, point, .19f + sprig * .024f,
                    (sprig % 2 == 0 ? -.30f : .34f), .18f * (trunk == 0 ? -1f : 1f),
                    .15f, sprig % 3 == 0 ? 1 : 0);
                accepted++;
            }
            for (int i = 0; i < 850 && accepted < 160; i++)
            {
                float x = -169.50f + (float)random.NextDouble() * 2.35f;
                float y = 5.16f + (float)random.NextDouble() * 4.04f;
                float lobe = Mathf.Min(Mathf.Abs(x + 168.94f), Mathf.Abs(x + 167.89f));
                float density = y < 6.35f ? .26f : y > 8.45f ? .84f : .68f;
                if (lobe > (.38f + .42f * Mathf.InverseLerp(5.1f, 9.2f, y)) ||
                    random.NextDouble() > density) continue;
                if (!Facade(c, x, y, out Vector3 point)) continue;
                point.z += .046f + (float)random.NextDouble() * .09f;
                float size = y < 6.35f ? .20f + (float)random.NextDouble() * .16f :
                    .27f + (float)random.NextDouble() * .27f;
                int variant = accepted % 5 == 0 ? 1 : accepted % 3 == 0 ? 2 : 0;
                float angle = (((float)random.NextDouble() - .5f) * .95f);
                leaves.ProfileLeaf(0, point, size, angle,
                    ((float)random.NextDouble() - .5f) * 1.4f,
                    ((float)random.NextDouble() - .5f) * 1.2f, variant);
                accepted++;
            }
            if (accepted < 100 || accepted > 160)
                throw new InvalidOperationException("Profile-leaf canopy/window clearance unexpected: " + accepted);
            // A small chipped plaster edge beside the growth origin, never on glass.
            bool patch = false;
            for (int k = 0; k < 28 && !patch; k++)
            {
                float x = -172.8f + (k % 14) * .45f;
                float y = k < 14 ? 6.05f : 8.18f;
                if (!Facade(c, x, y, out Vector3 p0) ||
                    !Facade(c, x + .53f, y, out Vector3 p1) ||
                    !Facade(c, x + .48f, y + .42f, out Vector3 p2) ||
                    !Facade(c, x + .07f, y + .37f, out Vector3 p3)) continue;
                loss.Quad(0, p0 + Vector3.forward * .016f, p1 + Vector3.forward * .016f,
                    p2 + Vector3.forward * .016f, p3 + Vector3.forward * .016f);
                patch = true;
            }
            if (!patch) throw new InvalidOperationException("Plaster chip patch hit glazing or missing wall");
        }

        static void Ribbon(MeshBook mesh, int slot, Vector3 a, Vector3 b, float width, Vector3 across)
        {
            Vector3 n = across.normalized * width * .5f;
            mesh.Quad(slot, a - n, b - n, b + n, a + n, true);
        }

        static Vector3[] GroundRim(Context c, float x, float outward,
            float radiusX, float radiusZ, int count, int seed, float offset)
        {
            var result = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float angle = 2f * Mathf.PI * i / count;
                float irregular = 1f + .11f * Mathf.Sin(i * 2.73f + seed * 1.31f) +
                    .055f * Mathf.Cos(i * 4.19f + seed);
                result[i] = Ground(c, x + Mathf.Cos(angle) * radiusX * irregular,
                    outward + Mathf.Sin(angle) * radiusZ * irregular, offset);
            }
            return result;
        }

        static Material[] CreateMaterials()
        {
            string[] sources = { ExistingRubble, ExistingStone, ExistingLeaf, ExistingPuddle,
                ExistingPlaster, ExistingStem, ExistingLeaf, ExistingStone };
            var colors = new[] {
                new Color(.51f,.47f,.37f), new Color(.72f,.70f,.61f), new Color(.54f,.47f,.29f),
                new Color(.25f,.29f,.26f), new Color(.78f,.77f,.68f), new Color(.37f,.33f,.27f),
                new Color(.83f,.91f,.76f), new Color(.52f,.54f,.52f) };
            var result = new Material[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                Material source = AssetDatabase.LoadAssetAtPath<Material>(sources[i]);
                if (!source) throw new InvalidOperationException("A34 source material missing: " + sources[i]);
                Material copy = new Material(source) { name = MaterialNames[i] };
                // Preserve the existing ivy leaf and stem values exactly. Their
                // copies are isolated only so later tuning never edits A33.
                if (i != 5 && i != 6)
                {
                    if (copy.HasProperty("_BaseColor")) copy.SetColor("_BaseColor", colors[i]);
                    else if (copy.HasProperty("_Color")) copy.SetColor("_Color", colors[i]);
                }
                if (i == 3 && copy.HasProperty("_Smoothness")) copy.SetFloat("_Smoothness", .52f);
                string path = AssetRoot + "/Materials/" + MaterialNames[i] + ".mat";
                AssetDatabase.CreateAsset(copy, path);
                result[i] = copy;
            }
            return result;
        }

        static Transform Child(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.layer = parent.gameObject.layer;
            return child.transform;
        }

        static void CreateMesh(Transform parent, string name, MeshBook book,
            Matrix4x4 worldToLocal, Material[] materials)
        {
            Mesh mesh = book.Make(name, worldToLocal);
            AssetDatabase.CreateAsset(mesh, AssetRoot + "/Meshes/" + name + ".asset");
            Transform child = Child(parent, name);
            child.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = child.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = name == "A34_GrowthStem" || name == "A34_GrowthLeaf" ?
                ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        static bool ValidExisting(Transform root, Renderer source)
        {
            if (!root || root.name != RootName || root.childCount != 2) return false;
            var ground = root.Find("GroundAndDeposits");
            var facade = root.Find("GrowthCanopy");
            if (!ground || !facade) return false;
            var marker = facade.GetComponent<SurfaceIvyInstance>();
            var binding = facade.GetComponent<AMapRuinIvyRevealBinding>();
            if (!marker || !binding || marker.sourceTarget != source.transform ||
                marker.sourceRenderers == null || marker.sourceRenderers.Length != 1 ||
                marker.sourceRenderers[0] != source) return false;
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length != 6 || root.GetComponentsInChildren<Collider>(true).Length != 0 ||
                root.GetComponentsInChildren<Light>(true).Length != 0) return false;
            foreach (string name in MeshNames)
            {
                Transform child = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
                if (!child || !child.GetComponent<MeshRenderer>() ||
                    AssetDatabase.GetAssetPath(child.GetComponent<MeshFilter>()?.sharedMesh) !=
                    AssetRoot + "/Meshes/" + name + ".asset") return false;
            }
            return true;
        }

        static IEnumerable<string> AssetPaths() =>
            MaterialNames.Select(n => AssetRoot + "/Materials/" + n + ".mat")
                .Concat(MeshNames.Select(n => AssetRoot + "/Meshes/" + n + ".asset"));

        static void DeleteOwnedAssets()
        {
            foreach (string path in AssetPaths())
                if (AssetDatabase.LoadMainAssetAtPath(path)) AssetDatabase.DeleteAsset(path);
            AssetDatabase.Refresh();
        }

        static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
