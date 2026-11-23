#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Escape.SurfaceIvy;

namespace Escape.EditorTools.Ivy
{
    /// <summary>Owned, unsaved output. Attach or save the meshes before disposing.</summary>
    public sealed class SurfaceIvyBuildResult : IDisposable
    {
        public Mesh stems, leaves;
        public List<Vector3> attachmentPointsLocal = new List<Vector3>();
        /// <summary>Ordered contact points for each root/branch path, in target-local space.</summary>
        public List<List<Vector3>> pathsLocal = new List<List<Vector3>>();
        public int sourceTriangles, paths, segments, leafCount, excludedSamples;
        public int exteriorRejectedSamples, rimSeededRoots;
        public int patchPlannedRoots, patchAcceptedRoots, activePatches;
        public int growthBlockedCrowding, growthBlockedExclusion;
        public int growthBlockedConnection, growthBlockedRim, growthNoSurface;
        public int growthStalledPaths;
        public string summary;
        public Bounds targetWorldBounds;

        public void Dispose()
        {
            DestroyUnsaved(stems);
            DestroyUnsaved(leaves);
            stems = leaves = null;
        }

        private static void DestroyUnsaved(Mesh mesh)
        {
            if (mesh != null && !EditorUtility.IsPersistent(mesh))
                UnityEngine.Object.DestroyImmediate(mesh);
        }
    }

    /// <summary>
    /// Deterministic editor-only ivy growth over visible source mesh triangles. The build
    /// reads the scene but neither creates objects nor changes meshes, importers or materials.
    /// The output is in target-local coordinates, while all distances are world metres.
    /// </summary>
    public static class SurfaceIvyGenerator
    {
        private const int MaxSourceTriangles = 250000;
        private const int MaxRoots = 128;
        private const int MaxSteps = 400;
        private const int MaxBranches = 256;
        private const int MaxLeaves = 16000;
        private const int MaxSegments = 48000;
        private const float MinArea2 = 0.00000001f;

        private struct Triangle
        {
            public Vector3 a, b, c, normal, center, min, max;
            public float area;
        }

        private struct Contact
        {
            public Vector3 point, normal;
            public int triangle;
            public float distance2;
        }

        private sealed class Node
        {
            public Vector3 min, max;
            public Node left, right;
            public int start, count;
        }

        private sealed class AxisComparer : IComparer<int>
        {
            internal readonly List<Triangle> triangles;
            internal int axis;
            public AxisComparer(List<Triangle> triangles) { this.triangles = triangles; }
            public int Compare(int x, int y)
            {
                float a = triangles[x].center[axis];
                float b = triangles[y].center[axis];
                int c = a.CompareTo(b);
                return c != 0 ? c : x.CompareTo(y);
            }
        }

        private sealed class MeshWriter
        {
            internal readonly List<Vector3> vertices = new List<Vector3>();
            internal readonly List<Vector2> uv = new List<Vector2>();
            internal readonly List<int> indices = new List<int>();
            internal void Add(Vector3 p, Vector2 tex) { vertices.Add(p); uv.Add(tex); }
            internal Mesh Finish(string name)
            {
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
                try
                {
                    mesh.SetVertices(vertices);
                    mesh.SetUVs(0, uv);
                    mesh.SetTriangles(indices, 0);
                    mesh.RecalculateNormals();
                    mesh.RecalculateBounds();
                    return mesh;
                }
                catch
                {
                    UnityEngine.Object.DestroyImmediate(mesh);
                    throw;
                }
            }
        }

        private sealed class LeafTemplate
        {
            internal Vector3[] vertices;
            internal Vector2[] uv;
            internal int[] indices;
        }

        private struct RandomStream
        {
            private uint state;
            internal RandomStream(int seed) { state = unchecked((uint)seed) + 0x9E3779B9u; if (state == 0) state = 1; }
            internal float Next()
            {
                uint x = state;
                x ^= x << 13; x ^= x >> 17; x ^= x << 5;
                state = x;
                return (x & 0xFFFFFFu) / 16777216f;
            }
            internal float Signed() { return Next() * 2f - 1f; }
        }

        private sealed class Path
        {
            internal Contact contact;
            internal Vector3 heading;
            internal int depth, steps, maxSteps, id, parentId, age, rimSteps;
            internal int leafRunLeft, leafGapLeft;
            internal float leafTravel;
            internal float radiusScale;
            internal List<Vector3> line;
        }

        private struct OccupiedContact
        {
            internal Vector3 point, normal;
            internal int pathId, step;
        }

        // Sparse world-space grid. Only nearby contacts are checked, so crowding
        // prevention remains bounded even on large source meshes.
        private sealed class GrowthOccupancy
        {
            private readonly Dictionary<Vector3Int, List<OccupiedContact>> cells =
                new Dictionary<Vector3Int, List<OccupiedContact>>();
            private readonly float cellSize, separation2;
            private readonly int samePathGrace, parentPathGrace;
            internal GrowthOccupancy(SurfaceIvySettings settings)
            {
                cellSize = Mathf.Max(0.09f, settings.leafSize * 1.35f, settings.stepLength * 1.1f);
                // An exterior wall's roots sit close together, while each new
                // contact is only about one stepLength from the previous one.
                // The generic leaf-sized clearance causes stems to stall at
                // their own third point and a branch to hit its parent.
                bool largeExterior = settings.preferExteriorVertical || settings.preferRoofRimClusters;
                float separation = largeExterior
                    ? Mathf.Clamp(settings.stepLength * .72f, .09f, .16f)
                    : cellSize;
                separation2 = separation * separation;
                samePathGrace = largeExterior ? 8 : 3;
                parentPathGrace = largeExterior ? 6 : 2;
            }
            private Vector3Int Cell(Vector3 p)
            {
                return new Vector3Int(Mathf.FloorToInt(p.x / cellSize),
                    Mathf.FloorToInt(p.y / cellSize), Mathf.FloorToInt(p.z / cellSize));
            }
            internal void Add(Vector3 point, Vector3 normal, int pathId, int step)
            {
                Vector3Int key = Cell(point);
                if (!cells.TryGetValue(key, out var list))
                {
                    list = new List<OccupiedContact>();
                    cells.Add(key, list);
                }
                list.Add(new OccupiedContact { point = point, normal = normal,
                    pathId = pathId, step = step });
            }
            internal bool Crowded(Vector3 point, Vector3 normal, Path path)
            {
                Vector3Int center = Cell(point);
                for (int z = -1; z <= 1; z++)
                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    var key = center + new Vector3Int(x, y, z);
                    if (!cells.TryGetValue(key, out var list)) continue;
                    for (int i = 0; i < list.Count; i++)
                    {
                        OccupiedContact old = list[i];
                        if (old.pathId == path.id && path.steps - old.step < samePathGrace) continue;
                        // A new branch initially shares its parent's attachment.
                        if (old.pathId == path.parentId && path.age < parentPathGrace) continue;
                        // A thin wall can legitimately carry separate ivy on both faces.
                        if (Vector3.Dot(normal, old.normal) < -0.45f) continue;
                        if ((point - old.point).sqrMagnitude < separation2) return true;
                    }
                }
                return false;
            }
        }

        public static SurfaceIvyBuildResult Build(GameObject target, SurfaceIvySettings settings,
            IList<Vector3> seedPointsLocal = null, IList<IvyExclusionBox> exclusions = null,
            Mesh customLeafMesh = null, Func<bool> cancelled = null)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            var result = new SurfaceIvyBuildResult();
            var triangles = new List<Triangle>();
            var reasons = new List<string>();
            var s = Sanitize(settings);
            Matrix4x4 toLocal = target.transform.worldToLocalMatrix;
            var rng = new RandomStream(s.seed);
            var leafStyleRng = new RandomStream(unchecked(s.seed ^ 0x51A11EAF));
            bool showProgress = false;
            try
            {
                ThrowIfCancelled(cancelled);
                Collect(target, triangles, reasons, ref showProgress, cancelled);
                result.sourceTriangles = triangles.Count;
                if (triangles.Count == 0)
                {
                    result.summary = "No eligible opaque MeshRenderer triangles. " + string.Join("; ", reasons);
                    return result;
                }

                var ids = new int[triangles.Count];
                for (int i = 0; i < ids.Length; i++) ids[i] = i;
                var comparer = new AxisComparer(triangles);
                Node root = BuildTree(triangles, ids, 0, ids.Length, comparer);
                var stems = new MeshWriter();
                var leaves = new MeshWriter();
                LeafTemplate leafTemplate = ReadLeafTemplate(customLeafMesh);
                var pending = new Queue<Path>();
                bool[] roofAllowed = s.preferRoofRimClusters ? new bool[triangles.Count] : null;
                Seed(target, triangles, ids, root, s, seedPointsLocal, exclusions,
                    roofAllowed, ref rng, result, pending);
                var occupancy = new GrowthOccupancy(s);
                foreach (Path seed in pending)
                    occupancy.Add(seed.contact.point, seed.contact.normal, seed.id, 0);
                int leafBudget = leafTemplate != null
                    ? Mathf.Min(s.maxLeaves, 1000000 / leafTemplate.vertices.Length,
                        4000000 / leafTemplate.indices.Length) : s.maxLeaves;
                int branchCount = 0;
                int pathAttempts = pending.Count;
                bool fairLargeBuilding = s.preferExteriorVertical || s.preferRoofRimClusters;
                while (pending.Count > 0 && result.segments < MaxSegments &&
                    (!s.stopAtLeafBudget || result.leafCount < leafBudget))
                {
                    var path = pending.Dequeue();
                    if (path.steps >= path.maxSteps) continue;
                    int remaining = path.maxSteps - path.steps;
                    // A large building should spend its global leaf budget
                    // across many anchored clusters, not finish the first root
                    // and its branches before reaching the next wall/roof edge.
                    if (fairLargeBuilding) remaining = Mathf.Min(remaining, 6);
                    bool stalled = false;
                    for (int step = 0; step < remaining && result.segments < MaxSegments &&
                        (!s.stopAtLeafBudget || result.leafCount < leafBudget); step++)
                    {
                        ThrowIfCancelled(cancelled);
                        if ((result.segments & 511) == 0) CheckCancel(ref showProgress, "Growing ivy", result.segments / (float)Mathf.Max(1, MaxSegments));
                        if (!TryStep(path, s, triangles, ids, root, toLocal, exclusions,
                                roofAllowed,
                                occupancy, result.targetWorldBounds,
                                ref rng, result, out Contact next, out Vector3 heading))
                        { stalled = true; result.growthStalledPaths++; break; }
                        bool nearTopRim = IsTopRim(path.contact, next, result.targetWorldBounds, s);
                        float tipProgress = path.age / (float)Mathf.Max(1, path.maxSteps - path.steps + path.age);
                        AddStem(stems, toLocal, path.contact, next, s.surfaceOffset,
                            Mathf.Max(.0015f, s.stemRadius * path.radiusScale * Mathf.Lerp(.78f, .36f, tipProgress)), result.segments);
                        result.segments++;
                        occupancy.Add(next.point, next.normal, path.id, path.steps + 1);
                        Vector3 localPoint = toLocal.MultiplyPoint3x4(next.point);
                        result.attachmentPointsLocal.Add(localPoint);
                        path.line.Add(localPoint);
                        path.leafTravel += Vector3.Distance(path.contact.point, next.point);
                        if (path.leafTravel >= s.leafSpacing && result.leafCount < leafBudget)
                        {
                            path.leafTravel -= s.leafSpacing;
                            if (path.leafGapLeft > 0) path.leafGapLeft--;
                            else
                            {
                                float clusteredSize = s.leafSize * (0.90f + leafStyleRng.Next() * 0.20f);
                                int clusterCount = path.leafRunLeft >= 2 && path.leafRunLeft <= 5 ? 2 : 1;
                                for (int cluster = 0; cluster < clusterCount && result.leafCount < leafBudget; cluster++)
                                    if (AddLeaf(leaves, toLocal, next, heading, clusteredSize,
                                            s.surfaceOffset + cluster * .003f, leafTemplate, exclusions, result, ref leafStyleRng))
                                        result.leafCount++;
                                path.leafRunLeft--;
                                if (path.leafRunLeft <= 0)
                                {
                                    path.leafGapLeft = rng.Next() < 0.85f ? 1 : 2;
                                    path.leafRunLeft = 4 + Mathf.FloorToInt(rng.Next() * 3f);
                                }
                            }
                        }
                        path.contact = next;
                        path.heading = heading;
                        path.steps++;
                        path.age++;
                        path.rimSteps = nearTopRim ? path.rimSteps + 1 : 0;
                        if (branchCount < s.maxBranches && pending.Count < MaxBranches &&
                            path.steps + 2 < path.maxSteps &&
                            path.steps > 2 && rng.Next() < s.branching)
                        {
                            Vector3 side = Vector3.Cross(path.contact.normal, heading).normalized;
                            if (side.sqrMagnitude > 0.5f)
                            {
                                int sign = rng.Next() < 0.5f ? -1 : 1;
                                var branchLine = new List<Vector3> { localPoint };
                                result.pathsLocal.Add(branchLine);
                                int branchBudget = path.steps + Mathf.Max(3,
                                    Mathf.RoundToInt((path.maxSteps - path.steps) * (0.60f + 0.32f * rng.Next())));
                                pending.Enqueue(new Path {
                                    contact = path.contact,
                                    heading = (heading * 0.48f + side * sign * 0.88f).normalized,
                                    depth = path.depth + 1,
                                    steps = path.steps,
                                    maxSteps = Mathf.Min(path.maxSteps, branchBudget),
                                    id = result.pathsLocal.Count - 1,
                                    parentId = path.id,
                                    leafTravel = s.leafSpacing * rng.Next() * 0.5f,
                                    leafRunLeft = 3 + Mathf.FloorToInt(rng.Next() * 3f),
                                    radiusScale = Mathf.Max(0.42f, path.radiusScale * 0.72f),
                                    line = branchLine
                                });
                                branchCount++;
                                pathAttempts++;
                            }
                        }
                    }
                    if (fairLargeBuilding && !stalled && path.steps < path.maxSteps &&
                        result.segments < MaxSegments &&
                        (!s.stopAtLeafBudget || result.leafCount < leafBudget))
                        pending.Enqueue(path);
                }
                result.paths = pathAttempts;
                result.stems = stems.Finish("Surface Ivy Stems");
                result.leaves = leaves.Finish("Surface Ivy Leaves");
                string largeBuildingReport = s.preferExteriorVertical || s.preferRoofRimClusters ?
                    $"{result.exteriorRejectedSamples} hidden facade samples, {result.rimSeededRoots} roof rim roots, {result.patchAcceptedRoots}/{result.patchPlannedRoots} planned patch roots across {result.activePatches} active patches. " +
                    $"Growth stalls {result.growthStalledPaths}: crowd {result.growthBlockedCrowding}, exclusions {result.growthBlockedExclusion}, surface connection {result.growthBlockedConnection}, rim {result.growthBlockedRim}, no projected surface {result.growthNoSurface}. " : "";
                result.summary = $"{triangles.Count} source triangles, {result.paths} paths, {result.segments} stem segments, {result.leafCount} leaves, {result.excludedSamples} excluded samples. " +
                    largeBuildingReport +
                    (result.patchPlannedRoots > 0 && result.patchAcceptedRoots <
                        Mathf.CeilToInt(result.patchPlannedRoots * Mathf.Clamp01(s.minPatchOccupancy))
                        ? "Patch occupancy below policy minimum. " : "") +
                    (leafBudget < s.maxLeaves ? "Custom leaf vertex budget limited leaf count. " : "") +
                    (customLeafMesh != null && leafTemplate == null ? "Unsupported custom leaf; used built-in folded leaf. " : "") +
                    (reasons.Count == 0 ? "" : "Skipped: " + string.Join("; ", reasons));
                return result;
            }
            catch
            {
                result.Dispose();
                throw;
            }
            finally
            {
                if (showProgress) EditorUtility.ClearProgressBar();
            }
        }

        private static SurfaceIvySettings Sanitize(SurfaceIvySettings source)
        {
            var s = source.Clone();
            s.roots = Mathf.Clamp(s.roots, 0, MaxRoots);
            s.steps = Mathf.Clamp(s.steps, 0, MaxSteps);
            s.maxBranches = Mathf.Clamp(s.maxBranches, 0, MaxBranches);
            s.maxLeaves = Mathf.Clamp(s.maxLeaves, 0, MaxLeaves);
            s.stepLength = ClampFinite(s.stepLength, 0.025f, 2f, 0.14f);
            s.maxProjectionDistance = ClampFinite(s.maxProjectionDistance, 0.01f, 2f, 0.35f);
            s.surfaceOffset = ClampFinite(s.surfaceOffset, 0.001f, 0.25f, 0.025f);
            s.stemRadius = ClampFinite(s.stemRadius, 0.001f, 0.1f, 0.012f);
            s.leafSize = ClampFinite(s.leafSize, 0.01f, 0.8f, 0.13f);
            s.leafSpacing = ClampFinite(s.leafSpacing, 0.025f, 1f, 0.1f);
            s.upwardBias = ClampFinite(s.upwardBias, -2f, 2f, 0.65f);
            s.branching = ClampFinite(s.branching, 0, 0.5f, 0.06f);
            s.spread = ClampFinite(s.spread, 0, 2f, 0.55f);
            return s;
        }

        private static float ClampFinite(float v, float min, float max, float fallback)
        {
            return float.IsNaN(v) || float.IsInfinity(v) ? fallback : Mathf.Clamp(v, min, max);
        }

        private static bool IsFinite(Vector3 p)
        {
            return !float.IsNaN(p.x) && !float.IsInfinity(p.x) &&
                   !float.IsNaN(p.y) && !float.IsInfinity(p.y) &&
                   !float.IsNaN(p.z) && !float.IsInfinity(p.z);
        }

        private static bool IsExcluded(Vector3 worldPoint, Matrix4x4 toLocal,
            IList<IvyExclusionBox> exclusions, float worldMargin, SurfaceIvyBuildResult result)
        {
            if (exclusions == null) return false;
            Vector3 local = toLocal.MultiplyPoint3x4(worldPoint);
            // IvyExclusionBox.Contains expects local units. A conservative conversion
            // keeps clearance under arbitrary rotation and nonuniform target scale.
            float margin = worldMargin * Mathf.Max(
                toLocal.MultiplyVector(Vector3.right).magnitude,
                toLocal.MultiplyVector(Vector3.up).magnitude,
                toLocal.MultiplyVector(Vector3.forward).magnitude);
            for (int i = 0; i < exclusions.Count; i++)
            {
                if (exclusions[i] != null && exclusions[i].Contains(local, margin))
                {
                    result.excludedSamples++;
                    return true;
                }
            }
            return false;
        }

        private static void CheckCancel(ref bool shown, string label, float fraction)
        {
            shown = true;
            if (EditorUtility.DisplayCancelableProgressBar("Surface Ivy", label, Mathf.Clamp01(fraction)))
                throw new OperationCanceledException("Surface Ivy generation cancelled.");
        }

        private static void ThrowIfCancelled(Func<bool> cancelled)
        {
            if (cancelled != null && cancelled())
                throw new OperationCanceledException("Surface Ivy generation cancelled.");
        }

        private static bool Opaque(Material material)
        {
            if (material == null) return true;
            if (material.renderQueue >= (int)RenderQueue.Transparent) return false;
            string type = material.GetTag("RenderType", false, "");
            if (type.IndexOf("Transparent", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            string name = material.name + " " + (material.shader == null ? "" : material.shader.name);
            if (name.IndexOf("glass", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("particle", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f) return false;
            return true;
        }

        private static LeafTemplate ReadLeafTemplate(Mesh source)
        {
            if (source == null) return null;
            try
            {
                using (var array = MeshUtility.AcquireReadOnlyMeshData(source))
                {
                    var data = array[0];
                    if (data.vertexCount < 3 || data.vertexCount > 256 || data.subMeshCount != 1 ||
                        !data.HasVertexAttribute(VertexAttribute.Position) ||
                        data.GetSubMesh(0).topology != MeshTopology.Triangles) return null;
                    int count = (int)data.GetSubMesh(0).indexCount;
                    if (count < 3 || count > 768 || count % 3 != 0) return null;
                    var verts = new NativeArray<Vector3>(data.vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                    var uv = new NativeArray<Vector2>(data.vertexCount, Allocator.Temp, NativeArrayOptions.ClearMemory);
                    var indices = new NativeArray<int>(count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                    try
                    {
                        data.GetVertices(verts);
                        if (data.HasVertexAttribute(VertexAttribute.TexCoord0)) data.GetUVs(0, uv);
                        data.GetIndices(indices, 0);
                        var template = new LeafTemplate { vertices = verts.ToArray(), uv = uv.ToArray(), indices = indices.ToArray() };
                        for (int i = 0; i < template.vertices.Length; i++)
                            if (!IsFinite(template.vertices[i])) return null;
                        for (int i = 0; i < template.indices.Length; i++)
                            if ((uint)template.indices[i] >= (uint)template.vertices.Length) return null;
                        return template;
                    }
                    finally { verts.Dispose(); uv.Dispose(); indices.Dispose(); }
                }
            }
            catch { return null; }
        }

        private static void Collect(GameObject target, List<Triangle> triangles,
            List<string> reasons, ref bool showProgress, Func<bool> cancelled)
        {
            MeshRenderer[] renderers = target.GetComponentsInChildren<MeshRenderer>(true);
            for (int ri = 0; ri < renderers.Length; ri++)
            {
                ThrowIfCancelled(cancelled);
                if ((ri & 15) == 0) CheckCancel(ref showProgress, "Reading source geometry", ri / (float)Mathf.Max(1, renderers.Length));
                var renderer = renderers[ri];
                if (!renderer.enabled || !ActiveFromTarget(renderer.transform, target.transform)) continue;
                if (renderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly) continue;
                if (renderer.GetComponentInParent<SurfaceIvyInstance>() != null) continue;
                var filter = renderer.GetComponent<MeshFilter>();
                Mesh mesh = filter == null ? null : filter.sharedMesh;
                if (mesh == null) { reasons.Add(renderer.name + ": no MeshFilter mesh"); continue; }
                if (triangles.Count >= MaxSourceTriangles) { reasons.Add("source triangle cap reached"); break; }
                try
                {
                    // Editor API also reads imported meshes with Read/Write disabled.
                    using (var dataArray = MeshUtility.AcquireReadOnlyMeshData(mesh))
                    {
                        var data = dataArray[0];
                        if (!data.HasVertexAttribute(VertexAttribute.Position))
                        { reasons.Add(renderer.name + ": no positions"); continue; }
                        var positions = new NativeArray<Vector3>(data.vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                        try
                        {
                            data.GetVertices(positions);
                            int submeshes = Mathf.Min(data.subMeshCount, mesh.subMeshCount);
                            Material[] materials = renderer.sharedMaterials;
                            Matrix4x4 toWorld = renderer.localToWorldMatrix;
                            for (int sub = 0; sub < submeshes; sub++)
                            {
                                if (data.GetSubMesh(sub).topology != MeshTopology.Triangles)
                                { reasons.Add(renderer.name + ": non-triangle submesh"); continue; }
                                Material mat = materials.Length == 0 ? null : materials[Mathf.Min(sub, materials.Length - 1)];
                                if (!Opaque(mat)) { reasons.Add(renderer.name + ": transparent/glass submesh"); continue; }
                                int count = (int)data.GetSubMesh(sub).indexCount;
                                var indices = new NativeArray<int>(count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                                try
                                {
                                    data.GetIndices(indices, sub);
                                    for (int i = 0; i + 2 < count && triangles.Count < MaxSourceTriangles; i += 3)
                                    {
                                        int ia = indices[i], ib = indices[i + 1], ic = indices[i + 2];
                                        if ((uint)ia >= (uint)positions.Length || (uint)ib >= (uint)positions.Length || (uint)ic >= (uint)positions.Length) continue;
                                        Vector3 a = toWorld.MultiplyPoint3x4(positions[ia]);
                                        Vector3 b = toWorld.MultiplyPoint3x4(positions[ib]);
                                        Vector3 c = toWorld.MultiplyPoint3x4(positions[ic]);
                                        if (!IsFinite(a) || !IsFinite(b) || !IsFinite(c)) continue;
                                        Vector3 cross = Vector3.Cross(b - a, c - a);
                                        float area2 = cross.magnitude;
                                        if (area2 < MinArea2) continue;
                                        Vector3 min = Vector3.Min(a, Vector3.Min(b, c));
                                        Vector3 max = Vector3.Max(a, Vector3.Max(b, c));
                                        triangles.Add(new Triangle { a = a, b = b, c = c, normal = cross / area2,
                                            area = area2 * 0.5f, center = (a + b + c) / 3f, min = min, max = max });
                                    }
                                }
                                finally { indices.Dispose(); }
                            }
                        }
                        finally { positions.Dispose(); }
                    }
                }
                catch (Exception e) when (!(e is OperationCanceledException))
                {
                    reasons.Add(renderer.name + ": unsupported mesh (" + e.GetType().Name + ")");
                }
            }
        }

        private static bool ActiveFromTarget(Transform child, Transform target)
        {
            for (Transform t = child; t != null; t = t.parent)
            {
                if (!t.gameObject.activeSelf) return false;
                if (t == target) return true;
            }
            return false;
        }

        private static Node BuildTree(List<Triangle> triangles, int[] ids, int start, int count, AxisComparer comparer)
        {
            var n = new Node { start = start, count = count,
                min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
                max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity) };
            for (int i = start; i < start + count; i++)
            {
                Triangle t = triangles[ids[i]];
                n.min = Vector3.Min(n.min, t.min);
                n.max = Vector3.Max(n.max, t.max);
            }
            if (count <= 12) return n;
            Vector3 size = n.max - n.min;
            comparer.axis = size.x >= size.y && size.x >= size.z ? 0 : (size.y >= size.z ? 1 : 2);
            Array.Sort(ids, start, count, comparer);
            int left = count / 2;
            n.left = BuildTree(triangles, ids, start, left, comparer);
            n.right = BuildTree(triangles, ids, start + left, count - left, comparer);
            return n;
        }

        private static float AabbDistance2(Vector3 p, Vector3 min, Vector3 max)
        {
            float x = Mathf.Max(min.x - p.x, 0, p.x - max.x);
            float y = Mathf.Max(min.y - p.y, 0, p.y - max.y);
            float z = Mathf.Max(min.z - p.z, 0, p.z - max.z);
            return x * x + y * y + z * z;
        }

        private static bool Nearest(Vector3 point, float radius, List<Triangle> triangles, int[] ids,
            Node node, out Contact result)
        {
            result = new Contact { triangle = -1, distance2 = radius * radius };
            Visit(point, triangles, ids, node, ref result);
            return result.triangle >= 0;
        }

        private static void Visit(Vector3 p, List<Triangle> triangles, int[] ids, Node node, ref Contact result)
        {
            if (node == null || AabbDistance2(p, node.min, node.max) > result.distance2) return;
            if (node.left != null)
            {
                float a = AabbDistance2(p, node.left.min, node.left.max);
                float b = AabbDistance2(p, node.right.min, node.right.max);
                if (a <= b) { Visit(p, triangles, ids, node.left, ref result); Visit(p, triangles, ids, node.right, ref result); }
                else { Visit(p, triangles, ids, node.right, ref result); Visit(p, triangles, ids, node.left, ref result); }
                return;
            }
            for (int i = node.start; i < node.start + node.count; i++)
            {
                int id = ids[i];
                Triangle tri = triangles[id];
                Vector3 q = ClosestPoint(p, tri.a, tri.b, tri.c);
                float d2 = (q - p).sqrMagnitude;
                if (d2 < result.distance2 - 1e-9f ||
                    (Mathf.Abs(d2 - result.distance2) < 1e-9f && id < result.triangle))
                    result = new Contact { point = q, normal = tri.normal, triangle = id, distance2 = d2 };
            }
        }

        // Ericson closest-point regions; unlike a bounding-box projection, it respects holes.
        private static Vector3 ClosestPoint(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = b - a, ac = c - a, ap = p - a;
            float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0 && d2 <= 0) return a;
            Vector3 bp = p - b;
            float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0 && d4 <= d3) return b;
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0 && d1 >= 0 && d3 <= 0) return a + ab * (d1 / (d1 - d3));
            Vector3 cp = p - c;
            float d5 = Vector3.Dot(ab, cp), d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0 && d5 <= d6) return c;
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0 && d2 >= 0 && d6 <= 0) return a + ac * (d2 / (d2 - d6));
            float va = d3 * d6 - d5 * d4;
            if (va <= 0 && d4 - d3 >= 0 && d5 - d6 >= 0)
                return b + (c - b) * ((d4 - d3) / ((d4 - d3) + (d5 - d6)));
            float inv = 1f / (va + vb + vc);
            return a + ab * (vb * inv) + ac * (vc * inv);
        }

        // Double-sided BVH first-hit rays decide whether an opaque source
        // triangle is actually exposed from outside. A bounding-box position
        // alone mistakes recessed courtyards and interior panels for facades.
        private static bool RayAabb(Vector3 origin, Vector3 direction, Vector3 min,
            Vector3 max, float maxDistance)
        {
            float near = 0f, far = maxDistance;
            for (int axis = 0; axis < 3; axis++)
            {
                float o = origin[axis], d = direction[axis];
                if (Mathf.Abs(d) < 1e-8f)
                {
                    if (o < min[axis] || o > max[axis]) return false;
                    continue;
                }
                float a = (min[axis] - o) / d, b = (max[axis] - o) / d;
                if (a > b) { float swap = a; a = b; b = swap; }
                near = Mathf.Max(near, a);
                far = Mathf.Min(far, b);
                if (near > far) return false;
            }
            return true;
        }

        private static bool RayTriangle(Vector3 origin, Vector3 direction,
            Triangle triangle, out float distance)
        {
            Vector3 ab = triangle.b - triangle.a, ac = triangle.c - triangle.a;
            Vector3 p = Vector3.Cross(direction, ac);
            float determinant = Vector3.Dot(ab, p);
            if (Mathf.Abs(determinant) < 1e-8f) { distance = 0f; return false; }
            float inverse = 1f / determinant;
            Vector3 t = origin - triangle.a;
            float u = Vector3.Dot(t, p) * inverse;
            if (u < -1e-5f || u > 1.00001f) { distance = 0f; return false; }
            Vector3 q = Vector3.Cross(t, ab);
            float v = Vector3.Dot(direction, q) * inverse;
            if (v < -1e-5f || u + v > 1.00001f) { distance = 0f; return false; }
            distance = Vector3.Dot(ac, q) * inverse;
            return distance > .0001f;
        }

        private static void VisitFirstRay(Vector3 origin, Vector3 direction,
            List<Triangle> triangles, int[] ids, Node node, ref int hit,
            ref float bestDistance)
        {
            if (node == null || !RayAabb(origin, direction, node.min, node.max,
                    bestDistance)) return;
            if (node.left != null)
            {
                VisitFirstRay(origin, direction, triangles, ids, node.left,
                    ref hit, ref bestDistance);
                VisitFirstRay(origin, direction, triangles, ids, node.right,
                    ref hit, ref bestDistance);
                return;
            }
            for (int i = node.start; i < node.start + node.count; i++)
            {
                int id = ids[i];
                if (!RayTriangle(origin, direction, triangles[id], out float distance) ||
                    distance >= bestDistance) continue;
                bestDistance = distance;
                hit = id;
            }
        }

        private static bool FirstHitNear(Vector3 source, Vector3 approach,
            float reach, float tolerance, List<Triangle> triangles, int[] ids, Node root)
        {
            Vector3 origin = source + approach * reach;
            Vector3 direction = -approach;
            float distance = reach + tolerance;
            int hit = -1;
            VisitFirstRay(origin, direction, triangles, ids, root, ref hit, ref distance);
            return hit >= 0 &&
                (origin + direction * distance - source).sqrMagnitude <= tolerance * tolerance;
        }

        private static bool ExteriorVertical(Vector3 point, Vector3 normal, Bounds bounds,
            List<Triangle> triangles, int[] ids, Node root)
        {
            if (Mathf.Abs(normal.y) > .50f) return false;
            Vector3 forward = new Vector3(normal.x, 0f, normal.z).normalized;
            if (forward.sqrMagnitude < .9f) return false;
            Vector3 side = new Vector3(-forward.z, 0f, forward.x);
            float reach = Mathf.Max(10f, bounds.size.magnitude * 1.5f + 4f);
            for (int sign = -1; sign <= 1; sign += 2)
            for (int skew = -1; skew <= 1; skew++)
            {
                Vector3 direction = (forward * sign + side * skew * .65f).normalized;
                if (FirstHitNear(point, direction, reach, .035f,
                    triangles, ids, root)) return true;
            }
            return false;
        }

        private static bool VisibleRoof(Vector3 point, Vector3 normal, Bounds bounds,
            List<Triangle> triangles, int[] ids, Node root)
        {
            if (Mathf.Abs(normal.y) < .35f) return false;
            float reach = Mathf.Max(4f, bounds.size.y + 4f);
            return FirstHitNear(point, Vector3.up, reach, .035f,
                triangles, ids, root);
        }

        private struct EdgeKey : IEquatable<EdgeKey>
        {
            internal int a, b;
            internal EdgeKey(int x, int y)
            { a = Mathf.Min(x, y); b = Mathf.Max(x, y); }
            public bool Equals(EdgeKey other) => a == other.a && b == other.b;
            public override bool Equals(object other) => other is EdgeKey edge && Equals(edge);
            public override int GetHashCode() => unchecked(a.GetHashCode() * 397 ^ b.GetHashCode());
        }

        private static Vector3Int RimCell(Vector3 p) => new Vector3Int(
            Mathf.FloorToInt(p.x * 1000f), Mathf.FloorToInt(p.y * 1000f),
            Mathf.FloorToInt(p.z * 1000f));

        private static int WeldRimPoint(Vector3 p, List<Vector3> points,
            Dictionary<Vector3Int, List<int>> cells)
        {
            Vector3Int cell = RimCell(p);
            const float tolerance2 = .001f * .001f;
            float best = tolerance2;
            int found = -1;
            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            for (int z = -1; z <= 1; z++)
            {
                if (!cells.TryGetValue(new Vector3Int(cell.x + x, cell.y + y,
                    cell.z + z), out var bucket)) continue;
                foreach (int id in bucket)
                {
                    float distance2 = (points[id] - p).sqrMagnitude;
                    if (distance2 > best) continue;
                    if (distance2 == best && found >= 0 && id >= found) continue;
                    best = distance2;
                    found = id;
                }
            }
            if (found >= 0) return found;
            int newId = points.Count;
            points.Add(p);
            if (!cells.TryGetValue(cell, out var own))
                cells.Add(cell, own = new List<int>(1));
            own.Add(newId);
            return newId;
        }

        private static bool[] RoofRimTriangles(List<Triangle> triangles)
        {
            var counts = new Dictionary<EdgeKey, int>();
            var points = new List<Vector3>();
            var cells = new Dictionary<Vector3Int, List<int>>();
            var vertices = new int[triangles.Count * 3];
            for (int i = 0; i < triangles.Count; i++)
            {
                Triangle t = triangles[i];
                if (Mathf.Abs(t.normal.y) < .35f) continue;
                int offset = i * 3;
                vertices[offset] = WeldRimPoint(t.a, points, cells);
                vertices[offset + 1] = WeldRimPoint(t.b, points, cells);
                vertices[offset + 2] = WeldRimPoint(t.c, points, cells);
                var edges = new[] {
                    new EdgeKey(vertices[offset], vertices[offset + 1]),
                    new EdgeKey(vertices[offset + 1], vertices[offset + 2]),
                    new EdgeKey(vertices[offset + 2], vertices[offset]) };
                foreach (EdgeKey edge in edges)
                { counts.TryGetValue(edge, out int n); counts[edge] = n + 1; }
            }
            var rim = new bool[triangles.Count];
            for (int i = 0; i < triangles.Count; i++)
            {
                Triangle t = triangles[i];
                if (Mathf.Abs(t.normal.y) < .35f) continue;
                int offset = i * 3;
                rim[i] = counts[new EdgeKey(vertices[offset], vertices[offset + 1])] == 1 ||
                    counts[new EdgeKey(vertices[offset + 1], vertices[offset + 2])] == 1 ||
                    counts[new EdgeKey(vertices[offset + 2], vertices[offset])] == 1;
            }
            return rim;
        }

        private static bool TrySampleRoot(List<Triangle> triangles, List<int> candidates,
            List<float> cumulative, float totalArea, int[] ids, Node root,
            SurfaceIvySettings s, SurfaceIvyClusterPatch patch, bool[] roofRim,
            Matrix4x4 toLocal, IList<IvyExclusionBox> exclusions,
            float minimumSpacing, ref RandomStream clusterRng,
            ref RandomStream growthRng,
            SurfaceIvyBuildResult result, Queue<Path> pending)
        {
            float pick = clusterRng.Next() * totalArea;
            int candidateIndex = cumulative.BinarySearch(pick);
            if (candidateIndex < 0) candidateIndex = ~candidateIndex;
            int triangleId = candidates[Mathf.Clamp(candidateIndex, 0, candidates.Count - 1)];
            Triangle tri = triangles[triangleId];
            float u = Mathf.Sqrt(clusterRng.Next()), v = clusterRng.Next();
            Vector3 point = tri.a * (1f - u) + tri.b * (u * (1f - v)) + tri.c * (u * v);
            if (patch != null)
            {
                Vector3 relative = Quaternion.Inverse(Quaternion.Euler(s.clusterFrameRotationEuler)) *
                    (point - s.clusterFrameCenterWorld);
                float pu = relative.x / Mathf.Max(.001f, s.clusterFootprintSizeWorld.x);
                float pv = relative.z / Mathf.Max(.001f, s.clusterFootprintSizeWorld.z);
                float radius = Mathf.Max(.001f, patch.radius);
                if (string.IsNullOrEmpty(patch.side))
                {
                    float dx = pu - patch.u, dz = pv - patch.v;
                    if (dx * dx + dz * dz > radius * radius) return false;
                }
                else
                {
                    float edge, along;
                    switch (patch.side.ToUpperInvariant())
                    {
                        case "N": edge = pv - .5f; along = pu; break;
                        case "S": edge = pv + .5f; along = pu; break;
                        case "E": edge = pu - .5f; along = pv; break;
                        case "W": edge = pu + .5f; along = pv; break;
                        default: return false;
                    }
                    if (Mathf.Abs(edge) > radius ||
                        Mathf.Abs(along - patch.along) > radius) return false;
                }
            }
            if (!s.includeRoof && tri.normal.y > .68f) return false;
            if (IsExcluded(point, toLocal, exclusions, 0f, result)) return false;
            if (s.preferExteriorVertical && !ExteriorVertical(point, tri.normal,
                result.targetWorldBounds, triangles, ids, root))
            { result.exteriorRejectedSamples++; return false; }
            var contact = new Contact { point = point, normal = tri.normal, triangle = triangleId };
            if (TooNearRoot(contact, pending, minimumSpacing)) return false;
            AddRoot(contact, s, ref growthRng, result, toLocal, pending);
            if (roofRim != null && roofRim[triangleId]) result.rimSeededRoots++;
            return true;
        }

        private static void Seed(GameObject target, List<Triangle> triangles, int[] ids, Node root,
            SurfaceIvySettings s, IList<Vector3> seeds, IList<IvyExclusionBox> exclusions,
            bool[] roofAllowed, ref RandomStream rng, SurfaceIvyBuildResult result, Queue<Path> pending)
        {
            Matrix4x4 toLocal = target.transform.worldToLocalMatrix;
            Matrix4x4 toWorld = target.transform.localToWorldMatrix;
            Vector3 min = triangles[0].min, max = triangles[0].max;
            for (int i = 0; i < triangles.Count; i++)
            {
                min = Vector3.Min(min, triangles[i].min);
                max = Vector3.Max(max, triangles[i].max);
            }
            result.targetWorldBounds = new Bounds((min + max) * 0.5f, max - min);
            bool[] roofRim = s.preferRoofRimClusters ? RoofRimTriangles(triangles) : null;
            var candidates = new List<int>(triangles.Count);
            var cumulative = new List<float>(triangles.Count);
            float totalArea = 0f;
            for (int i = 0; i < triangles.Count; i++)
            {
                Triangle tri = triangles[i];
                if (s.preferExteriorVertical && Mathf.Abs(tri.normal.y) > .50f) continue;
                if (s.preferRoofRimClusters &&
                    !VisibleRoof(tri.center, tri.normal, result.targetWorldBounds,
                        triangles, ids, root)) continue;
                if (roofAllowed != null) roofAllowed[i] = true;
                float weight = tri.area * (roofRim != null && roofRim[i] ? 6f : 1f);
                totalArea += weight;
                candidates.Add(i);
                cumulative.Add(totalArea);
            }
            if (candidates.Count == 0) return;
            int desired = s.roots;
            if (seeds != null && seeds.Count > 0)
            {
                desired = Mathf.Min(desired, seeds.Count);
                for (int i = 0; i < desired; i++)
                {
                    if (!IsFinite(seeds[i])) continue;
                    if (!Nearest(toWorld.MultiplyPoint3x4(seeds[i]), s.maxProjectionDistance, triangles, ids, root, out Contact c)) continue;
                    if ((!s.includeRoof && c.normal.y > 0.68f) || IsExcluded(c.point, toLocal, exclusions, 0, result)) continue;
                    if (s.preferExteriorVertical && !ExteriorVertical(c.point, c.normal,
                        result.targetWorldBounds, triangles, ids, root))
                    { result.exteriorRejectedSamples++; continue; }
                    if (roofAllowed != null && !roofAllowed[c.triangle]) continue;
                    AddRoot(c, s, ref rng, result, toLocal, pending);
                    if (roofRim != null && roofRim[c.triangle]) result.rimSeededRoots++;
                }
                return;
            }
            Vector3 boundsSize = result.targetWorldBounds.size;
            float nominalSpacing = Mathf.Clamp(boundsSize.magnitude /
                Mathf.Sqrt(Mathf.Max(1, desired)) * 0.25f, 0.16f, 1.8f);
            var patches = s.clusterPatches == null ? Array.Empty<SurfaceIvyClusterPatch>() :
                s.clusterPatches.Where(p => p != null && p.radius > 0f && p.weight > 0f).ToArray();
            int patchBudget = Mathf.Clamp(Mathf.RoundToInt(desired *
                (1f - Mathf.Clamp01(s.reserveAutomaticRootsFraction))), 0, desired);
            patchBudget = patches.Length == 0 ? 0 : patchBudget;
            float totalPatchWeight = patches.Sum(p => p.weight);
            int remainingPatchRoots = patchBudget;
            var validPatches = new List<SurfaceIvyClusterPatch>();
            var clusterRng = new RandomStream(s.clusterSeed != 0 ?
                s.clusterSeed : unchecked(s.seed ^ 0x3EAF21D5));
            for (int patchIndex = 0; patchIndex < patches.Length && remainingPatchRoots > 0; patchIndex++)
            {
                var patch = patches[patchIndex];
                int quota = patchIndex == patches.Length - 1 ? remainingPatchRoots :
                    Mathf.Clamp(Mathf.RoundToInt(patchBudget * patch.weight / totalPatchWeight),
                        patchBudget >= patches.Length ? 1 : 0, remainingPatchRoots);
                remainingPatchRoots -= quota;
                result.patchPlannedRoots += quota;
                int accepted = 0;
                int patchTrials = Mathf.Min(8192, Mathf.Max(96, quota * 128));
                for (int i = 0; i < patchTrials && accepted < quota; i++)
                {
                    float relaxation = i < patchTrials / 2 ? .75f : .45f;
                    if (TrySampleRoot(triangles, candidates, cumulative, totalArea,
                        ids, root, s, patch, roofRim, toLocal, exclusions,
                        nominalSpacing * relaxation, ref clusterRng, ref rng,
                        result, pending))
                    { accepted++; result.patchAcceptedRoots++; }
                }
                if (accepted > 0) { result.activePatches++; validPatches.Add(patch); }
            }
            // An inaccessible patch must not fail the whole building. Give its
            // share to other surface-supported patches before automatic roots.
            int shortfall = patchBudget - result.patchAcceptedRoots;
            int redistributionTrials = shortfall * 128;
            for (int i = 0; i < redistributionTrials && shortfall > 0 &&
                validPatches.Count > 0 && pending.Count < desired; i++)
            {
                var patch = validPatches[i % validPatches.Count];
                if (TrySampleRoot(triangles, candidates, cumulative, totalArea,
                    ids, root, s, patch, roofRim, toLocal, exclusions,
                    nominalSpacing * .40f, ref clusterRng, ref rng,
                    result, pending))
                { shortfall--; result.patchAcceptedRoots++; }
            }

            int trials = s.preferExteriorVertical ?
                Mathf.Min(8192, Mathf.Max(desired * 128, desired)) :
                s.preferRoofRimClusters ?
                Mathf.Min(4096, Mathf.Max(desired * 32, desired)) :
                Mathf.Min(4096, Mathf.Max(desired * 16, desired));
            for (int i = 0; i < trials && pending.Count < desired; i++)
            {
                float pick = rng.Next() * totalArea;
                int ix = cumulative.BinarySearch(pick);
                if (ix < 0) ix = ~ix;
                ix = Mathf.Clamp(ix, 0, candidates.Count - 1);
                int triangleId = candidates[ix];
                Triangle tri = triangles[triangleId];
                if (!s.includeRoof && tri.normal.y > .68f) continue;
                float u = Mathf.Sqrt(rng.Next()), v = rng.Next();
                Vector3 p = tri.a * (1f - u) + tri.b * (u * (1f - v)) + tri.c * (u * v);
                if (IsExcluded(p, toLocal, exclusions, 0f, result)) continue;
                if (s.preferExteriorVertical && !ExteriorVertical(p, tri.normal,
                    result.targetWorldBounds, triangles, ids, root))
                { result.exteriorRejectedSamples++; continue; }
                var c = new Contact { point = p, normal = tri.normal, triangle = triangleId };
                // Blue-noise-like acceptance over world positions. Relax late in
                // the finite trial budget so small/awkward meshes still get roots.
                float relaxation = i < trials / 2 ? 1f : (i < trials * 3 / 4 ? 0.7f : 0.45f);
                if (TooNearRoot(c, pending, nominalSpacing * relaxation)) continue;
                AddRoot(c, s, ref rng, result, toLocal, pending);
                if (roofRim != null && roofRim[triangleId]) result.rimSeededRoots++;
            }
        }

        private static bool TooNearRoot(Contact candidate, Queue<Path> pending, float minimum)
        {
            float limit2 = minimum * minimum;
            foreach (Path existing in pending)
            {
                // Opposing faces of a thin wall are distinct growth surfaces.
                float normalDot = Vector3.Dot(candidate.normal, existing.contact.normal);
                float needed2 = normalDot < -0.45f ? limit2 * 0.015f : limit2;
                if ((candidate.point - existing.contact.point).sqrMagnitude < needed2) return true;
            }
            return false;
        }

        private static void AddRoot(Contact c, SurfaceIvySettings settings, ref RandomStream rng, SurfaceIvyBuildResult result,
            Matrix4x4 toLocal, Queue<Path> pending)
        {
            Vector3 up = Vector3.ProjectOnPlane(Vector3.up, c.normal);
            if (up.sqrMagnitude < 0.01f) up = Vector3.ProjectOnPlane(Vector3.forward, c.normal);
            up.Normalize();
            Vector3 side = Vector3.Cross(c.normal, up).normalized;
            Vector3 heading = (up * (0.7f + rng.Next() * 0.3f) + side * rng.Signed() * 0.7f).normalized;
            Vector3 localPoint = toLocal.MultiplyPoint3x4(c.point);
            var line = new List<Vector3> { localPoint };
            result.pathsLocal.Add(line);
            int budget = settings.steps <= 0 ? 0 :
                Mathf.Clamp(Mathf.RoundToInt(settings.steps * (0.78f + rng.Next() * 0.36f)),
                    1, settings.steps);
            pending.Enqueue(new Path {
                contact = c, heading = heading, radiusScale = 1f, line = line,
                id = result.pathsLocal.Count - 1, parentId = -1, maxSteps = budget,
                leafTravel = settings.leafSpacing * rng.Next() * 0.65f,
                leafRunLeft = 4 + Mathf.FloorToInt(rng.Next() * 3f)
            });
            result.attachmentPointsLocal.Add(localPoint);
        }

        private static bool TryStep(Path path, SurfaceIvySettings s,
            List<Triangle> triangles, int[] ids, Node root, Matrix4x4 toLocal,
            IList<IvyExclusionBox> exclusions, bool[] roofAllowed,
            GrowthOccupancy occupancy, Bounds bounds,
            ref RandomStream rng, SurfaceIvyBuildResult result,
            out Contact next, out Vector3 heading)
        {
            next = default;
            heading = default;
            Contact previous = path.contact;
            Vector3 tangent = Vector3.ProjectOnPlane(path.heading, previous.normal);
            Vector3 up = Vector3.ProjectOnPlane(Vector3.up, previous.normal);
            Vector3 side = Vector3.Cross(previous.normal, tangent).normalized;
            tangent = (tangent + up * (s.upwardBias * 0.19f) + side * (rng.Signed() * s.spread * 0.24f)).normalized;
            if (tangent.sqrMagnitude < 0.1f) return false;

            // A small bidirectional normal look-ahead lets a stem turn around a
            // joined convex edge. The surface-attachment checks below reject a leap.
            float best = float.NegativeInfinity;
            Contact selected = default;
            bool found = false;
            float[] bends = { 0f, -0.5f, 0.5f, -1f, 1f };
            for (int k = 0; k < bends.Length; k++)
            {
                Vector3 probe = previous.point + tangent * s.stepLength + previous.normal * (s.stepLength * bends[k]);
                if (!Nearest(probe, Mathf.Min(s.maxProjectionDistance, s.stepLength * 1.35f), triangles, ids, root, out Contact candidate))
                { result.growthNoSurface++; continue; }
                Vector3 travel = candidate.point - previous.point;
                float dist = travel.magnitude;
                if (dist < s.stepLength * 0.28f || dist > s.stepLength * 1.45f)
                { result.growthNoSurface++; continue; }
                float progress = Vector3.Dot(travel / dist, tangent);
                if (progress < 0.32f || Vector3.Dot(previous.normal, candidate.normal) < -0.15f)
                { result.growthNoSurface++; continue; }
                if (!s.includeRoof && candidate.normal.y > 0.68f) continue;
                if (s.preferExteriorVertical && !ExteriorVertical(candidate.point,
                    candidate.normal, bounds, triangles, ids, root))
                { result.exteriorRejectedSamples++; continue; }
                if (roofAllowed != null && !roofAllowed[candidate.triangle])
                { result.growthNoSurface++; continue; }
                if (path.rimSteps >= 2 && IsTopRim(previous, candidate, bounds, s))
                { result.growthBlockedRim++; continue; }
                if (occupancy.Crowded(candidate.point, candidate.normal, path))
                { result.growthBlockedCrowding++; continue; }
                if (IsExcluded(candidate.point, toLocal, exclusions, s.stemRadius, result))
                { result.growthBlockedExclusion++; continue; }
                if (!ConnectedPath(previous, candidate, s, triangles, ids, root, toLocal, exclusions, result))
                { result.growthBlockedConnection++; continue; }
                float score = progress - Mathf.Abs(bends[k]) * 0.07f - candidate.distance2 / (s.stepLength * s.stepLength) * 0.03f;
                if (score > best) { best = score; selected = candidate; found = true; }
            }
            if (!found) return false;
            next = selected;
            heading = Vector3.ProjectOnPlane(next.point - previous.point, next.normal).normalized;
            if (heading.sqrMagnitude < 0.1f)
                heading = Vector3.ProjectOnPlane(tangent, next.normal).normalized;
            return heading.sqrMagnitude > 0.1f;
        }

        private static bool IsTopRim(Contact previous, Contact next, Bounds bounds, SurfaceIvySettings s)
        {
            if (bounds.size.y < 2.4f || Mathf.Max(bounds.size.x, bounds.size.z) < 3f) return false;
            if (previous.normal.y > 0.65f || next.normal.y > 0.65f) return false;
            Vector3 travel = next.point - previous.point;
            float topBand = Mathf.Max(0.24f, s.stepLength * 1.8f);
            return next.point.y >= bounds.max.y - topBand &&
                Mathf.Abs(travel.y) < s.stepLength * 0.32f;
        }

        private static bool ConnectedPath(Contact a, Contact b, SurfaceIvySettings s,
            List<Triangle> triangles, int[] ids, Node root, Matrix4x4 toLocal,
            IList<IvyExclusionBox> exclusions, SurfaceIvyBuildResult result)
        {
            // Multiple surface-supported waypoints reject paths spanning a hole,
            // door opening or detached nearby component. Offset scales with step.
            float support = Mathf.Min(s.maxProjectionDistance * 0.5f, s.stepLength * 0.38f, 0.09f);
            Contact last = a;
            for (int j = 1; j <= 4; j++)
            {
                Vector3 p = Vector3.Lerp(a.point, b.point, j / 4f);
                if (!Nearest(p, support, triangles, ids, root, out Contact c)) return false;
                if (Vector3.Dot(last.normal, c.normal) < -0.15f) return false;
                if (IsExcluded(c.point, toLocal, exclusions, s.stemRadius, result)) return false;
                last = c;
            }
            return true;
        }

        private static void AddStem(MeshWriter writer, Matrix4x4 toLocal, Contact a, Contact b,
            float surfaceOffset, float radius, int segment)
        {
            Vector3 start = a.point + a.normal * (surfaceOffset + radius);
            Vector3 end = b.point + b.normal * (surfaceOffset + radius);
            Vector3 axis = (end - start).normalized;
            Vector3 right = Vector3.Cross(axis, (a.normal + b.normal).normalized).normalized;
            if (right.sqrMagnitude < 0.1f) right = Vector3.Cross(axis, Vector3.up).normalized;
            if (right.sqrMagnitude < 0.1f) right = Vector3.right;
            Vector3 up = Vector3.Cross(axis, right).normalized;
            int baseIndex = writer.vertices.Count;
            const int sides = 5;
            for (int ring = 0; ring < 2; ring++)
            {
                Vector3 center = ring == 0 ? start : end;
                float r = radius * (ring == 0 ? 1f : 0.94f);
                for (int k = 0; k < sides; k++)
                {
                    float angle = k * Mathf.PI * 2f / sides;
                    Vector3 p = center + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * r;
                    writer.Add(toLocal.MultiplyPoint3x4(p), new Vector2(k / (float)sides, segment + ring));
                }
            }
            for (int k = 0; k < sides; k++)
            {
                int k1 = (k + 1) % sides;
                writer.indices.Add(baseIndex + k); writer.indices.Add(baseIndex + sides + k);
                writer.indices.Add(baseIndex + k1);
                writer.indices.Add(baseIndex + k1); writer.indices.Add(baseIndex + sides + k);
                writer.indices.Add(baseIndex + sides + k1);
            }
        }

        private static bool AddLeaf(MeshWriter writer, Matrix4x4 toLocal, Contact c,
            Vector3 heading, float size, float surfaceOffset, LeafTemplate custom,
            IList<IvyExclusionBox> exclusions, SurfaceIvyBuildResult result, ref RandomStream rng)
        {
            Vector3 normal = c.normal;
            Vector3 side = Vector3.Cross(normal, heading).normalized;
            if (side.sqrMagnitude < 0.1f) side = Vector3.Cross(normal, Vector3.up).normalized;
            float sign = (result.leafCount & 1) == 0 ? -1f : 1f;
            Vector3 longAxis = (heading * (0.30f + rng.Next() * 0.40f) + side * sign * (0.68f + rng.Next() * 0.25f)).normalized;
            Vector3 wideAxis = Vector3.Cross(normal, longAxis).normalized;
            float scale = size * (0.92f + rng.Next() * 0.42f);
            Vector3 origin = c.point + normal * (surfaceOffset + size * 0.055f);
            if (custom != null)
            {
                Vector3[] cv = custom.vertices;
                Vector2[] cuv = custom.uv;
                int[] ci = custom.indices;
                for (int i = 0; i < cv.Length; i++)
                {
                    Vector3 v = cv[i];
                    Vector3 p = origin + wideAxis * v.x * scale + longAxis * v.y * scale + normal * v.z * scale;
                    if (IsExcluded(p, toLocal, exclusions, 0, result)) return false;
                }
                int offset = writer.vertices.Count;
                for (int i = 0; i < cv.Length; i++)
                {
                    Vector3 v = cv[i];
                    writer.Add(toLocal.MultiplyPoint3x4(origin + wideAxis * v.x * scale +
                        longAxis * v.y * scale + normal * v.z * scale),
                        i < cuv.Length ? cuv[i] : new Vector2(v.x + 0.5f, v.y));
                }
                for (int i = 0; i < ci.Length; i++) writer.indices.Add(offset + ci[i]);
                return true;
            }
            float maturity = rng.Next();
            int variant = maturity < .18f ? 1 : (maturity > .76f ? 2 : 0);
            if (variant == 1) scale *= .78f;
            float asymmetry = rng.Signed();
            float curl = rng.Signed();
            for (int i = 0; i < SurfaceIvyLeafProfile.VertexCount; i++)
            {
                Vector3 v = SurfaceIvyLeafProfile.Evaluate(i, variant, asymmetry, curl);
                Vector3 p = origin + wideAxis * v.x * scale + longAxis * v.y * scale + normal * v.z * scale;
                if (IsExcluded(p, toLocal, exclusions, 0, result)) return false;
            }
            int start = writer.vertices.Count;
            for (int i = 0; i < SurfaceIvyLeafProfile.VertexCount; i++)
            {
                Vector3 v = SurfaceIvyLeafProfile.Evaluate(i, variant, asymmetry, curl);
                writer.Add(toLocal.MultiplyPoint3x4(origin + wideAxis * v.x * scale +
                    longAxis * v.y * scale + normal * v.z * scale), SurfaceIvyLeafProfile.UV(i, variant));
            }
            for (int i = 0; i < SurfaceIvyLeafProfile.BoundaryCount; i++)
            {
                // The leaf frame has cross(wideAxis,longAxis)=-normal; reverse local CCW fan.
                writer.indices.Add(start + (i + 1) % SurfaceIvyLeafProfile.BoundaryCount);
                writer.indices.Add(start + i);
                writer.indices.Add(start + SurfaceIvyLeafProfile.BoundaryCount);
            }
            return true;
        }
    }
}
#endif
