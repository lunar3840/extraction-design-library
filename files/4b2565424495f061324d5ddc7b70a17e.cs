using System;
using Escape.SurfaceIvy;
using UnityEngine;

namespace Escape.Stage.Blockout
{
    // Map-specific fallback for V33's material-based local reveal. V33 replaces
    // the source renderer's materials while it fades a wall around the player;
    // SurfaceIvyInstance only follows Renderer.enabled/active, so ivy otherwise
    // remains opaque over that revealed area. This component owns only the ivy
    // child renderers and runs after V33's LateUpdate (execution order 320).
    [DefaultExecutionOrder(500), DisallowMultipleComponent]
    [RequireComponent(typeof(SurfaceIvyInstance))]
    public sealed class AMapRuinIvyRevealBinding : MonoBehaviour
    {
        const string RevealShader = "Hidden/Escape/BuildingV33LocalReveal";
        [SerializeField] SurfaceIvyInstance ivy;
        MeshRenderer[] generated;
        bool[] authoredEnabled;
        bool revealHidden;
        bool originalFollowVisibility;
        StageAHallV13LayerVisibility hallLayers;
        bool mergedHallFacade;

        public void Configure(SurfaceIvyInstance instance)
        {
            if (!instance) throw new ArgumentNullException(nameof(instance));
            ivy = instance;
            generated = null;
            authoredEnabled = null;
            revealHidden = false;
            originalFollowVisibility = ivy.followSourceVisibility;
            CacheMergedHallFacade();
        }

        void OnEnable()
        {
            if (!ivy) ivy = GetComponent<SurfaceIvyInstance>();
            if (ivy) originalFollowVisibility = ivy.followSourceVisibility;
            CacheMergedHallFacade();
        }

        void CacheMergedHallFacade()
        {
            hallLayers = null;
            mergedHallFacade = false;
            if (!ivy || !ivy.sourceTarget || ivy.sourceTarget.name != "Facade") return;
            var map = ivy.sourceTarget.GetComponentInParent<StageABlockoutMap>();
            if (!map) return;
            hallLayers = map.GetComponent<StageAHallV13LayerVisibility>();
            mergedHallFacade = hallLayers && hallLayers.HallRoot &&
                ivy.sourceTarget == hallLayers.HallRoot.Find("Exterior/Facade");
        }

        void LateUpdate()
        {
            if (!ivy) return;
            if (SourceIsInV33Reveal()) HideGenerated();
            else if (revealHidden) RestoreGenerated();
        }

        void OnDisable()
        {
            if (revealHidden) RestoreGenerated();
        }

        bool SourceIsInV33Reveal()
        {
            // This one aggregate spans both floors. Hall cutaway hides upper
            // source panels individually, so an 'any source visible' test can
            // leave their ivy hanging over the indoor player. Interior vines
            // use separate groups and keep their authored visibility.
            if (mergedHallFacade && hallLayers && hallLayers.VisibleFloor != 99)
                return true;
            Renderer[] sources = ivy.sourceRenderers;
            if (sources == null) return false;
            foreach (Renderer source in sources)
            {
                if (!source || !source.enabled || !source.gameObject.activeInHierarchy) continue;
                foreach (Material material in source.sharedMaterials)
                    if (material && material.shader && material.shader.name == RevealShader)
                        return true;
            }
            return false;
        }

        void HideGenerated()
        {
            if (!revealHidden)
            {
                originalFollowVisibility = ivy.followSourceVisibility;
                ivy.followSourceVisibility = false;
                // Clears SurfaceIvyInstance's cached visibility state, so its
                // first refresh after reveal can restore authored renderers.
                ivy.RefreshVisibility();
                generated = GetComponentsInChildren<MeshRenderer>(true);
                authoredEnabled = new bool[generated.Length];
                for (int i = 0; i < generated.Length; i++)
                    authoredEnabled[i] = generated[i] && generated[i].enabled;
                revealHidden = true;
            }
            for (int i = 0; i < generated.Length; i++)
                if (generated[i]) generated[i].enabled = false;
        }

        void RestoreGenerated()
        {
            if (generated != null && authoredEnabled != null)
                for (int i = 0; i < generated.Length && i < authoredEnabled.Length; i++)
                    if (generated[i]) generated[i].enabled = authoredEnabled[i];
            revealHidden = false;
            if (ivy)
            {
                ivy.followSourceVisibility = originalFollowVisibility;
                ivy.RefreshVisibility();
            }
        }
    }
}
