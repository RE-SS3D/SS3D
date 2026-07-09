using System.Collections.Generic;
using Coimbra;
using SS3D.Core.Behaviours;
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
            Available,
            Unavailable,
        }

        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
        private const float OutlineWidth = 0.04f;
        private static readonly Color AvailableColor = new(0.15f, 0.95f, 0.35f, 1f);
        private static readonly Color UnavailableColor = new(0.98f, 0.88f, 0.15f, 1f);

        private static Material _outlineMaterial;

        private readonly List<OutlineEntry> _entries = new();
        private OutlineState _currentState = OutlineState.Hidden;
        private bool _built;

        private struct OutlineEntry
        {
            public Renderer Renderer;
            public MaterialPropertyBlock PropertyBlock;
        }

        public void SetState(OutlineState state)
        {
            if (!_built)
            {
                Build();
            }

            if (_entries.Count == 0)
            {
                return;
            }

            if (_currentState == state)
            {
                return;
            }

            _currentState = state;

            if (state == OutlineState.Hidden)
            {
                SetRenderersEnabled(false);
                return;
            }

            Color color = state == OutlineState.Available ? AvailableColor : UnavailableColor;

            foreach (OutlineEntry entry in _entries)
            {
                entry.PropertyBlock.SetColor(OutlineColorId, color);
                entry.Renderer.SetPropertyBlock(entry.PropertyBlock);
                entry.Renderer.enabled = true;
            }
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            foreach (OutlineEntry entry in _entries)
            {
                if (entry.Renderer != null)
                {
                    entry.Renderer.gameObject.Dispose(true);
                }
            }

            _entries.Clear();
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

            Shader shader = Shader.Find("Custom/InteractionOutline");
            if (shader == null)
            {
                return;
            }

            _outlineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            _outlineMaterial.SetFloat(OutlineWidthId, OutlineWidth);
        }

        private void CollectOutlineRenderers(Transform root, Selectable ownerSelectable)
        {
            if (_outlineMaterial == null)
            {
                return;
            }

            if (root.name == "InteractionOutline")
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

            GameObject outlineObject = new("InteractionOutline");
            outlineObject.transform.SetParent(source.transform, false);
            outlineObject.layer = source.gameObject.layer;

            MeshFilter outlineFilter = outlineObject.AddComponent<MeshFilter>();
            outlineFilter.sharedMesh = meshFilter.sharedMesh;

            MeshRenderer outlineRenderer = outlineObject.AddComponent<MeshRenderer>();
            outlineRenderer.sharedMaterial = _outlineMaterial;
            outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            outlineRenderer.enabled = false;

            _entries.Add(new OutlineEntry
            {
                Renderer = outlineRenderer,
                PropertyBlock = new MaterialPropertyBlock(),
            });
        }

        private void TryAddSkinnedOutline(SkinnedMeshRenderer source)
        {
            if (source.sharedMesh == null)
            {
                return;
            }

            GameObject outlineObject = new("InteractionOutline");
            outlineObject.transform.SetParent(source.transform, false);
            outlineObject.layer = source.gameObject.layer;

            SkinnedMeshRenderer outlineRenderer = outlineObject.AddComponent<SkinnedMeshRenderer>();
            outlineRenderer.sharedMesh = source.sharedMesh;
            outlineRenderer.sharedMaterials = new[] { _outlineMaterial };
            outlineRenderer.bones = source.bones;
            outlineRenderer.rootBone = source.rootBone;
            outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            outlineRenderer.enabled = false;

            _entries.Add(new OutlineEntry
            {
                Renderer = outlineRenderer,
                PropertyBlock = new MaterialPropertyBlock(),
            });
        }

        private void SetRenderersEnabled(bool enabled)
        {
            foreach (OutlineEntry entry in _entries)
            {
                entry.Renderer.enabled = enabled;
            }
        }
    }
}
