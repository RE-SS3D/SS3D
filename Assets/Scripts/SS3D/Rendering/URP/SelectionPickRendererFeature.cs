using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace SS3D.Rendering.URP
{
    /// <summary>
    /// Renders selectable objects with the Custom/Selection shader into an offscreen target
    /// for CPU colour readback by <see cref="Systems.Selection.SelectionCamera"/>.
    /// Draws registered <see cref="SelectionPickContext.ISelectionPickSource"/> meshes with a
    /// transient MaterialPropertyBlock so world MeshRenderers stay MPB-free for SRP Batcher.
    /// </summary>
    public sealed class SelectionPickRendererFeature : ScriptableRendererFeature
    {
        static readonly int SelectionColorId = Shader.PropertyToID("_SelectionColor");

        [SerializeField] private Shader _selectionShader;

        SelectionPickRenderPass _pickPass;
        SelectionPickDebugBlitPass _debugBlitPass;
        Material _selectionMaterial;

        public override void Create()
        {
            if (_selectionShader == null)
            {
                _selectionShader = Shader.Find("Custom/Selection");
            }

            if (_selectionShader != null && _selectionMaterial == null)
            {
                _selectionMaterial = CoreUtils.CreateEngineMaterial(_selectionShader);
            }

            _pickPass = new SelectionPickRenderPass(_selectionMaterial)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingOpaques
            };

            _debugBlitPass = new SelectionPickDebugBlitPass
            {
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_selectionMaterial == null || !SelectionPickContext.TryGetRequest(out var request))
            {
                return;
            }

            if (renderingData.cameraData.camera != request.SourceCamera)
            {
                return;
            }

            _pickPass.Setup(request);
            renderer.EnqueuePass(_pickPass);

            if (request.DebugView)
            {
                _debugBlitPass.Setup(request);
                renderer.EnqueuePass(_debugBlitPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            _pickPass?.DisposePass();
            _debugBlitPass?.DisposePass();
            CoreUtils.Destroy(_selectionMaterial);
        }

        sealed class SelectionPickRenderPass : ScriptableRenderPass
        {
            readonly Material _selectionMaterial;
            readonly List<SelectionPickContext.PickDraw> _draws = new();
            readonly List<SelectionPickContext.PickDraw> _opaqueDraws = new();
            readonly List<SelectionPickContext.PickDraw> _transparentDraws = new();
            readonly MaterialPropertyBlock _propertyBlock = new();

            SelectionPickContext.Request _request;
            RTHandle _importedTarget;

            public SelectionPickRenderPass(Material selectionMaterial)
            {
                _selectionMaterial = selectionMaterial;
                profilingSampler = new ProfilingSampler("SS3D Selection Pick");
            }

            public void Setup(SelectionPickContext.Request request)
            {
                _request = request;
            }

            public void DisposePass()
            {
                _importedTarget?.Release();
                _importedTarget = null;
            }

            void EnsureImportedTarget()
            {
                if (_importedTarget == null || _importedTarget.rt != _request.Target)
                {
                    _importedTarget?.Release();
                    _importedTarget = RTHandles.Alloc(_request.Target);
                }
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (_selectionMaterial == null || _request.Target == null)
                {
                    return;
                }

                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                EnsureImportedTarget();
                TextureHandle pickTarget = renderGraph.ImportTexture(_importedTarget);
                bool useSceneDepth = cameraData.cameraTargetDescriptor.msaaSamples <= 1
                    && resourceData.activeDepthTexture.IsValid();

                SelectionPickContext.CollectPickDraws(_draws);

                RecordDrawPass(
                    renderGraph,
                    resourceData,
                    pickTarget,
                    useSceneDepth,
                    transparent: false,
                    materialPassIndex: 0,
                    clearTarget: true,
                    "SS3D Selection Pick Opaque");

                RecordDrawPass(
                    renderGraph,
                    resourceData,
                    pickTarget,
                    useSceneDepth,
                    transparent: true,
                    materialPassIndex: 1,
                    clearTarget: false,
                    "SS3D Selection Pick Transparent");
            }

            void RecordDrawPass(
                RenderGraph renderGraph,
                UniversalResourceData resourceData,
                TextureHandle pickColor,
                bool useSceneDepth,
                bool transparent,
                int materialPassIndex,
                bool clearTarget,
                string passName)
            {
                // Snapshot matching draws for this pass (list is filled once per frame in RecordRenderGraph).
                // Reuse scratch lists — Render Graph holds the reference until execute.
                List<SelectionPickContext.PickDraw> passDraws = transparent ? _transparentDraws : _opaqueDraws;
                passDraws.Clear();
                for (int i = 0; i < _draws.Count; i++)
                {
                    if (_draws[i].Transparent != transparent)
                        continue;

                    passDraws.Add(_draws[i]);
                }

                if (passDraws.Count == 0)
                {
                    if (!clearTarget)
                        return;

                    // Still clear the pick target when there are no opaque selectables.
                    using var clearBuilder = renderGraph.AddRasterRenderPass<ClearPassData>(
                        passName + " Clear",
                        out ClearPassData clearData,
                        profilingSampler);
                    clearData.ClearTarget = true;
                    clearBuilder.SetRenderAttachment(pickColor, 0, AccessFlags.Write);
                    if (useSceneDepth)
                        clearBuilder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                    clearBuilder.SetRenderFunc((ClearPassData data, RasterGraphContext context) =>
                    {
                        context.cmd.ClearRenderTarget(RTClearFlags.Color, Color.black, 1, 0);
                    });
                    return;
                }

                using var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out PassData passData, profilingSampler);
                passData.Draws = passDraws;
                passData.Material = _selectionMaterial;
                passData.MaterialPassIndex = materialPassIndex;
                passData.PropertyBlock = _propertyBlock;
                passData.ClearTarget = clearTarget;
                passData.SelectionColorId = SelectionColorId;

                builder.SetRenderAttachment(pickColor, 0, AccessFlags.Write);

                if (useSceneDepth)
                {
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                }

                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    if (data.ClearTarget)
                    {
                        context.cmd.ClearRenderTarget(RTClearFlags.Color, Color.black, 1, 0);
                    }

                    MaterialPropertyBlock block = data.PropertyBlock;
                    List<SelectionPickContext.PickDraw> draws = data.Draws;
                    for (int i = 0; i < draws.Count; i++)
                    {
                        SelectionPickContext.PickDraw draw = draws[i];

                        if (draw.SkinnedRenderer != null)
                        {
                            // Skinned meshes keep a permanent selection MPB set at register time;
                            // DrawRenderer applies it. Do not SetPropertyBlock here (render thread).
                            context.cmd.DrawRenderer(
                                draw.SkinnedRenderer,
                                data.Material,
                                draw.SubmeshIndex,
                                data.MaterialPassIndex);
                        }
                        else if (draw.Mesh != null)
                        {
                            block.Clear();
                            block.SetColor(data.SelectionColorId, draw.Color);
                            context.cmd.DrawMesh(
                                draw.Mesh,
                                draw.Matrix,
                                data.Material,
                                draw.SubmeshIndex,
                                data.MaterialPassIndex,
                                block);
                        }
                    }
                });
            }

            class PassData
            {
                public List<SelectionPickContext.PickDraw> Draws;
                public Material Material;
                public int MaterialPassIndex;
                public MaterialPropertyBlock PropertyBlock;
                public bool ClearTarget;
                public int SelectionColorId;
            }

            class ClearPassData
            {
                public bool ClearTarget;
            }
        }

        sealed class SelectionPickDebugBlitPass : ScriptableRenderPass
        {
            SelectionPickContext.Request _request;
            RTHandle _importedTarget;

            public void Setup(SelectionPickContext.Request request)
            {
                _request = request;
            }

            public void DisposePass()
            {
                _importedTarget?.Release();
                _importedTarget = null;
            }

            void EnsureImportedTarget()
            {
                if (_importedTarget == null || _importedTarget.rt != _request.Target)
                {
                    _importedTarget?.Release();
                    _importedTarget = RTHandles.Alloc(_request.Target);
                }
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (_request.Target == null)
                {
                    return;
                }

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                EnsureImportedTarget();
                TextureHandle source = renderGraph.ImportTexture(_importedTarget);

                RenderGraphUtils.BlitMaterialParameters blitParams = new(
                    source,
                    resourceData.activeColorTexture,
                    Blitter.GetBlitMaterial(TextureDimension.Tex2D),
                    0);
                renderGraph.AddBlitPass(blitParams, passName: "SS3D Selection Pick Debug Blit");
            }
        }
    }
}
