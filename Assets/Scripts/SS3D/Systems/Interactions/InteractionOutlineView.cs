using System.Collections.Generic;
using Coimbra;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using SS3D.Rendering.URP;
using SS3D.Systems.Selection;
using UnityEngine;
using UnityEngine.Rendering;
using Actor = SS3D.Core.Behaviours.Actor;

namespace SS3D.Systems.Interactions
{
    /// <summary>
    /// Renders a coloured mesh outline for interaction feedback on a <see cref="Selectable"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionOutlineView : Actor
    {
        public enum OutlineState
        {
            Hidden,
            Pending,
            Available,
            Unavailable,
        }

        /// <summary>Name of the child GameObjects that hold outline shells.</summary>
        public const string OutlineObjectName = "InteractionOutline";

        private static readonly HashSet<int> PendingSelectableIds = new();
        private static readonly Dictionary<int, InteractionOutlineView> PendingViews = new();

        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
        /// <summary>Screen pixels; the shader extrudes the hull in clip space so this holds at any distance.</summary>
        private const float OutlineWidth = 2f;
        private static readonly Color PendingColor = new(0.55f, 0.85f, 1f, 1f);
        private static readonly Color AvailableColor = new(0.15f, 0.95f, 0.35f, 1f);
        private static readonly Color UnavailableColor = new(0.98f, 0.88f, 0.15f, 1f);

        private static readonly int CullId = Shader.PropertyToID("_Cull");
        private static readonly int OffsetFactorId = Shader.PropertyToID("_OffsetFactor");
        private static readonly int OffsetUnitsId = Shader.PropertyToID("_OffsetUnits");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");

        /// <summary>A mesh flatter than this (world metres) is a floor or plating: it gets the fill, not the hull.</summary>
        private const float FlatMeshHeight = 0.12f;
        private const float FillAlpha = 0.35f;

        private static Material _outlineMaterial;
        private static Material _fillMaterial;

        private readonly List<OutlineEntry> _entries = new();
        private OutlineState _currentState = OutlineState.Hidden;
        private bool _built;

        private struct OutlineEntry
        {
            public Renderer Renderer;
            public MaterialPropertyBlock PropertyBlock;
            public bool Fill;
        }

        public void SetState(OutlineState state)
        {
            if (!_built)
            {
                Build();
            }

            PruneDestroyedEntries();

            if (_entries.Count == 0)
            {
                _currentState = OutlineState.Hidden;
                return;
            }

            if (state == OutlineState.Hidden)
            {
                // Always force renderers off, even if _currentState already reads Hidden: external code
                // (e.g. Item.SetVisibility) can force-enable renderers without going through this view.
                _currentState = state;
                SetRenderersEnabled(false);
                return;
            }

            if (_currentState == state)
            {
                return;
            }

            _currentState = state;

            Color color = state switch
            {
                OutlineState.Pending => PendingColor,
                OutlineState.Available => AvailableColor,
                OutlineState.Unavailable => UnavailableColor,
                _ => AvailableColor,
            };

            foreach (OutlineEntry entry in _entries)
            {
                if (entry.Renderer == null)
                {
                    continue;
                }

                entry.PropertyBlock.SetColor(OutlineColorId, entry.Fill ? new Color(color.r, color.g, color.b, FillAlpha) : color);
                entry.Renderer.SetPropertyBlock(entry.PropertyBlock);
                entry.Renderer.enabled = true;
            }
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            int selectableId = GetComponent<Selectable>()?.GetInstanceID() ?? 0;
            if (selectableId != 0)
            {
                PendingSelectableIds.Remove(selectableId);
                PendingViews.Remove(selectableId);
            }

            foreach (OutlineEntry entry in _entries)
            {
                if (entry.Renderer != null)
                {
                    entry.Renderer.gameObject.Dispose(true);
                }
            }

            _entries.Clear();
        }

        /// <summary>
        /// Outline shells own their own <see cref="Renderer.enabled"/> state. Bulk renderer toggles
        /// (map editor show-all, layer dimming, item visibility) must skip them, or every object that
        /// was ever hovered keeps a visible outline.
        /// </summary>
        public static bool IsOutlineRenderer(Renderer renderer) =>
            renderer != null && renderer.transform.name == OutlineObjectName;

        public static bool IsPending(Selectable selectable)
        {
            return selectable != null && PendingSelectableIds.Contains(selectable.GetInstanceID());
        }

        public static void TryBeginPending(IInteraction interaction, InteractionEvent interactionEvent)
        {
            if (interaction is IDelayedInteraction)
            {
                return;
            }

            if (!TryGetSelectable(interactionEvent, out Selectable selectable))
            {
                return;
            }

            InteractionOutlineView view = GetOrCreate(selectable);
            int selectableId = selectable.GetInstanceID();
            PendingSelectableIds.Add(selectableId);
            PendingViews[selectableId] = view;
            view.SetState(OutlineState.Pending);
        }

        public static void ClearPendingExcept(Selectable hovered)
        {
            int hoveredId = hovered != null ? hovered.GetInstanceID() : 0;

            foreach (int selectableId in new List<int>(PendingSelectableIds))
            {
                if (selectableId == hoveredId)
                {
                    continue;
                }

                ClearPendingView(selectableId);
            }
        }

        public static void ClearPending()
        {
            foreach (int selectableId in new List<int>(PendingSelectableIds))
            {
                ClearPendingView(selectableId);
            }

            PendingSelectableIds.Clear();
            PendingViews.Clear();
        }

        private static void ClearPendingView(int selectableId)
        {
            if (PendingViews.TryGetValue(selectableId, out InteractionOutlineView view) && view != null)
            {
                view.SetState(OutlineState.Hidden);
            }

            PendingSelectableIds.Remove(selectableId);
            PendingViews.Remove(selectableId);
        }

        public static InteractionOutlineView GetOrCreate(Selectable selectable)
        {
            if (!selectable.TryGetComponent(out InteractionOutlineView outlineView))
            {
                outlineView = selectable.gameObject.AddComponent<InteractionOutlineView>();
            }

            return outlineView;
        }

        private static bool TryGetSelectable(InteractionEvent interactionEvent, out Selectable selectable)
        {
            selectable = null;

            if (interactionEvent?.Target is not IGameObjectProvider provider)
            {
                return false;
            }

            selectable = provider.GameObject.GetComponentInParent<Selectable>();
            return selectable != null;
        }

        private void Build()
        {
            EnsureMaterial();
            CollectOutlineRenderers(transform, GetComponent<Selectable>());
            _built = true;
        }

        private static void EnsureMaterial()
        {
            if (_outlineMaterial != null)
            {
                return;
            }

            // Must stay in GraphicsSettings Always Included Shaders — no prefab/material
            // reference pulls this into player builds, and Shader.Find then returns null.
            Shader shader = Shader.Find("Custom/InteractionOutline");
            if (shader == null)
            {
                Log.Warning(
                    typeof(InteractionOutlineView),
                    "Custom/InteractionOutline missing from the player (stripped). Add it to Always Included Shaders.",
                    Logs.Important);
                return;
            }

            _outlineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            _outlineMaterial.SetFloat(OutlineWidthId, OutlineWidth);

            // Same shader, so no second Always Included entry: a flat tile's normals face the camera and a
            // hull draws nothing, so it gets a translucent tint drawn just in front of its own surface.
            _fillMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            _fillMaterial.SetFloat(OutlineWidthId, 0f);
            _fillMaterial.SetFloat(CullId, (float)CullMode.Back);
            _fillMaterial.SetFloat(OffsetFactorId, -1f);
            _fillMaterial.SetFloat(OffsetUnitsId, -1f);
            _fillMaterial.SetFloat(SrcBlendId, (float)BlendMode.SrcAlpha);
            _fillMaterial.SetFloat(DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
        }

        private void CollectOutlineRenderers(Transform root, Selectable ownerSelectable)
        {
            if (_outlineMaterial == null)
            {
                return;
            }

            if (root.name == OutlineObjectName)
            {
                return;
            }

            Selectable nestedSelectable = root.GetComponent<Selectable>();
            if (nestedSelectable != null && nestedSelectable != ownerSelectable)
            {
                return;
            }

            MeshRenderer meshRenderer = root.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                TryAddMeshOutline(meshRenderer);
            }

            SkinnedMeshRenderer skinnedMeshRenderer = root.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                TryAddSkinnedOutline(skinnedMeshRenderer);
            }

            for (int i = 0; i < root.childCount; i++)
            {
                CollectOutlineRenderers(root.GetChild(i), ownerSelectable);
            }
        }

        private void TryAddMeshOutline(MeshRenderer source)
        {
            MeshFilter meshFilter = source.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                return;
            }

            GameObject outlineObject = new(OutlineObjectName);
            outlineObject.transform.SetParent(source.transform, false);
            outlineObject.layer = source.gameObject.layer;

            MeshFilter outlineFilter = outlineObject.AddComponent<MeshFilter>();
            outlineFilter.sharedMesh = meshFilter.sharedMesh;

            bool fill = source.bounds.size.y < FlatMeshHeight;
            MeshRenderer outlineRenderer = outlineObject.AddComponent<MeshRenderer>();
            outlineRenderer.sharedMaterial = fill ? _fillMaterial : _outlineMaterial;
            outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            outlineRenderer.renderingLayerMask = SelectionRenderingLayers.ExcludeFromSelectionPick;
            outlineRenderer.enabled = false;

            _entries.Add(new OutlineEntry
            {
                Renderer = outlineRenderer,
                PropertyBlock = new MaterialPropertyBlock(),
                Fill = fill,
            });
        }

        private void TryAddSkinnedOutline(SkinnedMeshRenderer source)
        {
            if (source.sharedMesh == null)
            {
                return;
            }

            GameObject outlineObject = new(OutlineObjectName);
            outlineObject.transform.SetParent(source.transform, false);
            outlineObject.layer = source.gameObject.layer;

            SkinnedMeshRenderer outlineRenderer = outlineObject.AddComponent<SkinnedMeshRenderer>();
            outlineRenderer.sharedMesh = source.sharedMesh;
            outlineRenderer.sharedMaterials = new[] { _outlineMaterial };
            outlineRenderer.bones = source.bones;
            outlineRenderer.rootBone = source.rootBone;
            outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            outlineRenderer.renderingLayerMask = SelectionRenderingLayers.ExcludeFromSelectionPick;
            outlineRenderer.enabled = false;

            _entries.Add(new OutlineEntry
            {
                Renderer = outlineRenderer,
                PropertyBlock = new MaterialPropertyBlock(),
            });
        }

        private void SetRenderersEnabled(bool enabled)
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                Renderer renderer = _entries[i].Renderer;
                if (renderer == null)
                {
                    _entries.RemoveAt(i);
                    continue;
                }

                renderer.enabled = enabled;
            }
        }

        /// <summary>
        /// Outline MeshRenderers are parented under source meshes. Structural destruction can wipe
        /// those children while this view (on the Selectable) still lives — drop stale refs.
        /// </summary>
        private void PruneDestroyedEntries()
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (_entries[i].Renderer == null)
                {
                    _entries.RemoveAt(i);
                }
            }
        }
    }
}
