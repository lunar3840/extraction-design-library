using System;
using System.Collections.Generic;
using UnityEngine;

namespace Escape.SurfaceIvy
{
    [Serializable]
    public sealed class SurfaceIvySettings
    {
        public int seed = 731;
        public int roots = 12;
        public int steps = 65;
        public int maxBranches = 24;
        public int maxLeaves = 1800;
        public float stepLength = .14f;
        public float maxProjectionDistance = .35f;
        public float surfaceOffset = .025f;
        public float stemRadius = .012f;
        public float leafSize = .13f;
        public float leafSpacing = .10f;
        public float upwardBias = .65f;
        public float branching = .06f;
        public float spread = .55f;
        public bool includeRoof = true;
        public bool stopAtLeafBudget = false;

        public SurfaceIvySettings Clone() => (SurfaceIvySettings)MemberwiseClone();

        public void Sanitize()
        {
            roots = Mathf.Clamp(roots, 1, 128);
            steps = Mathf.Clamp(steps, 1, 300);
            maxBranches = Mathf.Clamp(maxBranches, 0, 128);
            maxLeaves = Mathf.Clamp(maxLeaves, 0, 12000);
            stepLength = FiniteClamp(stepLength, .015f, 1.5f, .14f);
            maxProjectionDistance = FiniteClamp(maxProjectionDistance, .025f, 2f, .35f);
            surfaceOffset = FiniteClamp(surfaceOffset, .001f, .15f, .025f);
            stemRadius = FiniteClamp(stemRadius, .002f, .08f, .012f);
            leafSize = FiniteClamp(leafSize, .015f, 1f, .13f);
            leafSpacing = FiniteClamp(leafSpacing, .015f, 1f, .10f);
            upwardBias = FiniteClamp(upwardBias, -1f, 1f, .65f);
            branching = FiniteClamp(branching, 0f, 1f, .06f);
            spread = FiniteClamp(spread, 0f, 1f, .55f);
        }

        private static float FiniteClamp(float value, float min, float max, float fallback) =>
            float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);

        public static SurfaceIvySettings Vehicle() => new SurfaceIvySettings
        {
            seed = 731, roots = 14, steps = 52, maxBranches = 22, maxLeaves = 1300,
            stepLength = .11f, maxProjectionDistance = .26f, surfaceOffset = .024f,
            stemRadius = .009f, leafSize = .105f, leafSpacing = .085f,
            upwardBias = .34f, branching = .07f, spread = .68f, includeRoof = true
        };

        public static SurfaceIvySettings Wall() => new SurfaceIvySettings
        {
            seed = 731, roots = 12, steps = 95, maxBranches = 30, maxLeaves = 2100,
            stepLength = .14f, maxProjectionDistance = .38f, surfaceOffset = .026f,
            stemRadius = .012f, leafSize = .15f, leafSpacing = .11f,
            upwardBias = .78f, branching = .065f, spread = .48f, includeRoof = false
        };

        public static SurfaceIvySettings Roof() => new SurfaceIvySettings
        {
            seed = 731, roots = 10, steps = 68, maxBranches = 20, maxLeaves = 1500,
            stepLength = .16f, maxProjectionDistance = .3f, surfaceOffset = .03f,
            stemRadius = .011f, leafSize = .14f, leafSpacing = .11f,
            upwardBias = .08f, branching = .055f, spread = .80f, includeRoof = true
        };
    }

    [Serializable]
    public sealed class IvyExclusionBox
    {
        public Vector3 center;
        public Vector3 size = Vector3.one;
        public Vector3 rotation;

        public bool Contains(Vector3 targetLocalPoint, float margin = 0f)
        {
            Vector3 delta = Quaternion.Inverse(Quaternion.Euler(rotation)) * (targetLocalPoint - center);
            Vector3 half = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z)) * .5f;
            margin = Mathf.Max(0f, margin);
            return Mathf.Abs(delta.x) <= half.x + margin &&
                   Mathf.Abs(delta.y) <= half.y + margin &&
                   Mathf.Abs(delta.z) <= half.z + margin;
        }
    }

    // Lives on the generated child, never on the source object. No growth or geometry work runs in play mode.
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class SurfaceIvyInstance : MonoBehaviour
    {
        public Transform sourceTarget;
        public SurfaceIvySettings settings = new SurfaceIvySettings();
        public List<Vector3> seedPointsLocal = new List<Vector3>();
        public List<IvyExclusionBox> exclusions = new List<IvyExclusionBox>();
        public Material stemMaterial;
        public Material leafMaterial;
        public Mesh customLeafMesh;
        public Renderer[] sourceRenderers = Array.Empty<Renderer>();
        public bool followSourceVisibility = true;
        [HideInInspector] public bool isPreview = true;
        [HideInInspector] public bool ownsWorkingCopy;
        [HideInInspector] public string generatedAssetFolder;
        [TextArea(2, 8)] public string generationReport;
        [HideInInspector] public Mesh[] ownedPreviewMeshes = Array.Empty<Mesh>();
        [HideInInspector] public Material[] ownedPreviewMaterials = Array.Empty<Material>();
        [HideInInspector] public Texture2D ownedPreviewTexture;

        private Renderer[] generatedRenderers;
        [SerializeField, HideInInspector] private bool[] authoredRendererState;
        private float nextVisibilityCheck;
        private bool? lastVisible;

        private void OnEnable()
        {
            generatedRenderers = null;
            lastVisible = null;
        }

        private void OnDisable()
        {
            RestoreAuthoredVisibility();
        }

        private void LateUpdate()
        {
            if (!followSourceVisibility || sourceRenderers == null || sourceRenderers.Length == 0)
                return;
            if (Time.realtimeSinceStartup < nextVisibilityCheck)
                return;
            nextVisibilityCheck = Time.realtimeSinceStartup + .20f;
            RefreshVisibility();
        }

        public void RefreshVisibility()
        {
            if (!followSourceVisibility)
            {
                RestoreAuthoredVisibility();
                return;
            }
            if (sourceRenderers == null || sourceRenderers.Length == 0)
                return;

            bool anyKnownVisible = false;
            int checkedCount = 0;
            for (int i = 0; i < sourceRenderers.Length && checkedCount < 64; ++i)
            {
                Renderer source = sourceRenderers[i];
                if (source == null) continue;
                checkedCount++;
                if (source.enabled && !source.forceRenderingOff && source.gameObject.activeInHierarchy)
                {
                    anyKnownVisible = true;
                    break;
                }
            }
            if (checkedCount == 0 || lastVisible == anyKnownVisible)
                return;

            if (generatedRenderers == null)
            {
                generatedRenderers = GetComponentsInChildren<Renderer>(true);
                if (authoredRendererState == null || authoredRendererState.Length != generatedRenderers.Length)
                {
                    authoredRendererState = new bool[generatedRenderers.Length];
                    for (int i = 0; i < generatedRenderers.Length; ++i)
                        authoredRendererState[i] = generatedRenderers[i] != null && generatedRenderers[i].enabled;
                }
            }
            for (int i = 0; i < generatedRenderers.Length; ++i)
                if (generatedRenderers[i] != null)
                    generatedRenderers[i].enabled = anyKnownVisible && authoredRendererState[i];
            lastVisible = anyKnownVisible;
        }

        private void RestoreAuthoredVisibility()
        {
            if (generatedRenderers == null || authoredRendererState == null) return;
            for (int i = 0; i < generatedRenderers.Length && i < authoredRendererState.Length; ++i)
                if (generatedRenderers[i] != null) generatedRenderers[i].enabled = authoredRendererState[i];
            lastVisible = null;
        }
    }
}
