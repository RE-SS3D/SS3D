using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using SS3D.Systems.Selection;

namespace SS3D.Rendering.URP
{
    /// <summary>
    /// Renders selectable objects with the Custom/Selection shader into an offscreen target
    /// for CPU colour readback by <see cref="Systems.Selection.SelectionCamera"/>.
    /// </summary>
    public sealed class SelectionPickRendererFeature : ScriptableRendererFeature
    {
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
            readonly Material _overrideMaterial;
            readonly List<ShaderTagId> _shaderTags = new()
            {
                new ShaderTagId("ForwardBase"),
                new ShaderTagId("ForwardAdd"),
                new ShaderTagId("Always"),
                new ShaderTagId(string.Empty),
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("SRPDefaultUnlit"),
            };

            SelectionPickContext.Request _request;
            RTHandle _importedTarget;

            public SelectionPickRenderPass(Material overrideMaterial)
            {
                _overrideMaterial = overrideMaterial;
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
                if (_overrideMaterial == null || _request.Target == null)
                {
                    return;
                }

                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                EnsureImportedTarget();
                TextureHandle pickTarget = renderGraph.ImportTexture(_importedTarget);
                bool useSceneDepth = cameraData.cameraTargetDescriptor.msaaSamples <= 1
                    && resourceData.activeDepthTexture.IsValid();

                RecordQueuePass(
                    renderGraph,
                    resourceData,
                    renderingData,
                    cameraData,
                    lightData,
                    pickTarget,
                    useSceneDepth,
                    RenderQueueRange.opaque,
                    cameraData.defaultOpaqueSortFlags,
                    materialPassIndex: 0,
                    clearTarget: true,
                    "SS3D Selection Pick Opaque");

                RecordQueuePass(
                    renderGraph,
                    resourceData,
                    renderingData,
                    cameraData,
                    lightData,
                    pickTarget,
                    useSceneDepth,
                    RenderQueueRange.transparent,
                    SortingCriteria.CommonTransparent,
                    materialPassIndex: 1,
                    clearTarget: false,
                    "SS3D Selection Pick Transparent");
            }

            void RecordQueuePass(
                RenderGraph renderGraph,
                UniversalResourceData resourceData,
                UniversalRenderingData renderingData,
                UniversalCameraData cameraData,
                UniversalLightData lightData,
                TextureHandle pickColor,
                bool useSceneDepth,
                RenderQueueRange queueRange,
                SortingCriteria sortFlags,
                int materialPassIndex,
                bool clearTarget,
                string passName)
            {
                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(
                    _shaderTags,
                    renderingData,
                    cameraData,
                    lightData,
                    sortFlags);
                drawingSettings.overrideMaterial = _overrideMaterial;
                drawingSettings.overrideMaterialPassIndex = materialPassIndex;

                FilteringSettings filteringSettings = new FilteringSettings(queueRange, cameraData.camera.cullingMask)
                {
                    renderingLayerMask = SelectionRenderingLayers.PickPassMask
                };

                RendererListHandle rendererList;
                if (useSceneDepth)
                {
                    var rendererListParams = new RendererListParams(
                        renderingData.cullResults,
                        drawingSettings,
                        filteringSettings);
                    rendererList = renderGraph.CreateRendererList(rendererListParams);
                }
                else
                {
                    var renderStateBlock = new RenderStateBlock(RenderStateMask.Depth);
                    renderStateBlock.depthState = new DepthState(false, CompareFunction.Always);

                    var tagValues = new NativeArray<ShaderTagId>(1, Allocator.Temp);
                    var stateBlocks = new NativeArray<RenderStateBlock>(1, Allocator.Temp);
                    tagValues[0] = ShaderTagId.none;
                    stateBlocks[0] = renderStateBlock;

                    var rendererListParams = new RendererListParams(
                        renderingData.cullResults,
                        drawingSettings,
                        filteringSettings)
                    {
                        tagValues = tagValues,
                        stateBlocks = stateBlocks,
                        isPassTagName = false
                    };
                    rendererList = renderGraph.CreateRendererList(rendererListParams);
                }

                if (!rendererList.IsValid())
                {
                    return;
                }

                using var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out PassData passData, profilingSampler);
                passData.RendererList = rendererList;

                builder.UseRendererList(passData.RendererList);
                builder.SetRenderAttachment(pickColor, 0, AccessFlags.Write);

                if (useSceneDepth)
                {
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                }

                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    if (clearTarget)
                    {
                        context.cmd.ClearRenderTarget(RTClearFlags.Color, Color.black, 1, 0);
                    }

                    context.cmd.DrawRendererList(data.RendererList);
                });
            }

            class PassData
            {
                public RendererListHandle RendererList;
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
