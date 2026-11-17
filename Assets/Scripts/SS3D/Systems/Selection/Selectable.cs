using System.Collections.Generic;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Rendering.URP;
using SS3D.Systems.Tile;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Systems.Selection
{
    [DisallowMultipleComponent]
    public class Selectable : Actor, SelectionPickContext.ISelectionPickSource
    {
        static readonly int SelectionColorId = Shader.PropertyToID("_SelectionColor");
        static MaterialPropertyBlock s_materialBlockScratch;

        Color32 _selectionColor;
        readonly List<MeshFilter> _meshFilters = new();
        readonly List<MeshRenderer> _meshRenderers = new();
        readonly List<SkinnedMeshRenderer> _skinnedRenderers = new();
        bool _pickCacheBuilt;
        bool _canMove;

        /// <summary>
        /// The color that this Selectable will be rendered by the Selection Camera
        /// </summary>
        public Color32 SelectionColor
        {
            get => _selectionColor;
            set => _selectionColor = value;
        }

        public Vector3 PickWorldCenter => transform.position;

        public bool PreferAlwaysScan
        {
            get
            {
                EnsurePickCache();
                return _skinnedRenderers.Count > 0;
            }
        }

        /// <summary>
        /// Anything not placed on the tile map: items, characters and other free objects. Tile
        /// objects make up nearly every source and stay put, so the pick index never polls them.
        /// </summary>
        public bool CanMove => _canMove;

        protected override void OnStart()
        {
            base.OnStart();
            _selectionColor = SubSystems.Get<SelectionSubSystem>().RegisterSelectable(this);
            EnsurePickCache();
            _canMove = GetComponentInParent<PlacedTileObject>(true) == null;
            SelectionPickContext.RegisterSource(this);
        }

        protected override void OnDestroyed()
        {
            SelectionPickContext.UnregisterSource(this);
            base.OnDestroyed();
        }

        public void CollectPickDraws(List<SelectionPickContext.PickDraw> buffer)
        {
            if (!isActiveAndEnabled)
                return;

            EnsurePickCache();

            for (int i = 0; i < _meshRenderers.Count; i++)
            {
                MeshRenderer renderer = _meshRenderers[i];
                MeshFilter filter = _meshFilters[i];
                if (renderer == null || filter == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                if (IsExcludedFromPick(renderer))
                    continue;

                // DrawMesh bypasses Unity's MeshRenderer frustum cull — AOI only disables
                // off-observer tiles. Only draw selectables whose bounds the cursor ray hits.
                Bounds bounds = renderer.bounds;
                if (!SelectionPickContext.IsInPickFrustum(bounds)
                    || !SelectionPickContext.IsNearPickCursor(bounds))
                    continue;

                Mesh mesh = filter.sharedMesh;
                if (mesh == null)
                    continue;

                bool transparent = IsTransparent(renderer);
                Matrix4x4 matrix = renderer.localToWorldMatrix;
                int submeshCount = Mathf.Min(mesh.subMeshCount, renderer.sharedMaterials.Length);
                if (submeshCount <= 0)
                    submeshCount = mesh.subMeshCount;

                for (int submesh = 0; submesh < submeshCount; submesh++)
                {
                    bool submeshTransparent = transparent;
                    if (renderer.sharedMaterials != null
                        && submesh < renderer.sharedMaterials.Length)
                    {
                        submeshTransparent = IsMaterialTransparent(renderer.sharedMaterials[submesh]);
                    }

                    buffer.Add(new SelectionPickContext.PickDraw
                    {
                        Mesh = mesh,
                        Matrix = matrix,
                        Color = _selectionColor,
                        SubmeshIndex = submesh,
                        Transparent = submeshTransparent,
                        SkinnedRenderer = null,
                    });
                }
            }

            for (int i = 0; i < _skinnedRenderers.Count; i++)
            {
                SkinnedMeshRenderer renderer = _skinnedRenderers[i];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                if (IsExcludedFromPick(renderer))
                    continue;

                Bounds bounds = renderer.bounds;
                if (!SelectionPickContext.IsInPickFrustum(bounds)
                    || !SelectionPickContext.IsNearPickCursor(bounds))
                    continue;

                bool transparent = IsTransparent(renderer);
                int submeshCount = renderer.sharedMesh != null
                    ? Mathf.Min(renderer.sharedMesh.subMeshCount, Mathf.Max(1, renderer.sharedMaterials.Length))
                    : Mathf.Max(1, renderer.sharedMaterials.Length);
                SyncSkinnedSelectionColor(renderer, _selectionColor, submeshCount);
                for (int submesh = 0; submesh < submeshCount; submesh++)
                {
                    buffer.Add(new SelectionPickContext.PickDraw
                    {
                        Mesh = null,
                        Matrix = Matrix4x4.identity,
                        Color = _selectionColor,
                        SubmeshIndex = submesh,
                        Transparent = transparent,
                        SkinnedRenderer = renderer,
                    });
                }
            }
        }

        void EnsurePickCache()
        {
            if (_pickCacheBuilt)
                return;

            CachePickRenderers(gameObject, this);
            _pickCacheBuilt = true;
        }

        void CachePickRenderers(GameObject go, Selectable initial)
        {
            Selectable current = go.GetComponent<Selectable>();
            if (current != null && current != initial)
                return;

            SkinnedMeshRenderer skinned = go.GetComponent<SkinnedMeshRenderer>();
            if (skinned != null)
            {
                _skinnedRenderers.Add(skinned);
                // Skinned pick uses DrawRenderer, which reads the renderer's MPB — few characters.
                ApplySkinnedSelectionColor(skinned, _selectionColor);
            }
            else
            {
                MeshFilter filter = go.GetComponent<MeshFilter>();
                MeshRenderer renderer = go.GetComponent<MeshRenderer>();
                if (filter != null && renderer != null)
                {
                    _meshFilters.Add(filter);
                    _meshRenderers.Add(renderer);
                    // Do not leave a permanent selection MPB — that breaks SRP Batcher / GPU Instancing.
                    ClearSelectionColorFromBlock(renderer);
                }
            }

            foreach (Transform child in go.transform)
                CachePickRenderers(child.gameObject, initial);
        }

        static void ApplySkinnedSelectionColor(Renderer renderer, Color32 color)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor(SelectionColorId, color);
            renderer.SetPropertyBlock(block);
        }

        /// <summary>
        /// A per-material property block replaces the renderer-wide one for its material, so a skinned mesh whose
        /// material another system drives per material (power emission through <c>EmissiveMaterialSlots</c>) lost
        /// the renderer-wide selection colour and drew black in the pick pass. Copies the colour into every
        /// per-material block that exists; materials without one still read the renderer-wide block.
        /// </summary>
        public static void SyncSkinnedSelectionColor(Renderer renderer, Color32 color, int materialCount)
        {
            s_materialBlockScratch ??= new MaterialPropertyBlock();
            Color wanted = color;
            for (int i = 0; i < materialCount; i++)
            {
                renderer.GetPropertyBlock(s_materialBlockScratch, i);
                if (s_materialBlockScratch.isEmpty
                    || (s_materialBlockScratch.HasColor(SelectionColorId) && SameColor(s_materialBlockScratch.GetColor(SelectionColorId), color)))
                    continue;

                s_materialBlockScratch.SetColor(SelectionColorId, wanted);
                renderer.SetPropertyBlock(s_materialBlockScratch, i);
            }
        }

        // Picks match by exact Color32; a block hands the colour back as floats that need not round-trip exactly.
        static bool SameColor(Color stored, Color32 wanted)
        {
            Color32 rounded = stored;
            return rounded.r == wanted.r && rounded.g == wanted.g && rounded.b == wanted.b && rounded.a == wanted.a;
        }

        static void ClearSelectionColorFromBlock(Renderer renderer)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            // Other systems own blocks too (hair/eyebrow tint): only drop one carrying a selection color.
            if (block.isEmpty || !block.HasColor(SelectionColorId))
                return;

            // Drop leftover selection MPBs from older builds. Integrity re-applies when damaged.
            renderer.SetPropertyBlock(null);
        }

        static bool IsExcludedFromPick(Renderer renderer)
        {
            return (renderer.renderingLayerMask & SelectionRenderingLayers.ExcludeFromSelectionPick) != 0;
        }

        static bool IsTransparent(Renderer renderer)
        {
            Material[] materials = renderer.sharedMaterials;
            if (materials == null)
                return false;

            for (int i = 0; i < materials.Length; i++)
            {
                if (IsMaterialTransparent(materials[i]))
                    return true;
            }

            return false;
        }

        static bool IsMaterialTransparent(Material material)
        {
            return material != null && material.renderQueue >= (int)RenderQueue.Transparent;
        }
    }
}
