using UnityEngine;
using System;

namespace UnityStandardAssets.CinematicEffects
{
    //Improvement ideas:
    //  Use rgba8 buffer in ldr / in some pass in hdr (in correlation to previous point and remapping coc from -1/0/1 to 0/0.5/1)
    //  Use temporal stabilisation
    //  Add a mode to do bokeh texture in quarter res as well
    //  Support different near and far blur for the bokeh texture
    //  Try distance field for the bokeh texture
    //  Try to separate the output of the blur pass to two rendertarget near+far, see the gain in quality vs loss in performance
    //  Try swirl effect on the samples of the circle blur

    //References :
    //  This DOF implementation use ideas from public sources, a big thank to them :
    //  http://www.iryoku.com/next-generation-post-processing-in-call-of-duty-advanced-warfare
    //  http://www.crytek.com/download/Sousa_Graphics_Gems_CryENGINE3.pdf
    //  http://graphics.cs.williams.edu/papers/MedianShaderX6/
    //  http://http.developer.nvidia.com/GPUGems/gpugems_ch24.html
    //  http://vec3.ca/bicubic-filtering-in-fewer-taps/

    [ExecuteInEditMode]
    [AddComponentMenu("Image Effects/Cinematic/Depth Of Field")]
    [RequireComponent(typeof(Camera))]
    public class DepthOfField : MonoBehaviour
    {
        private const float kMaxBlur = 40.0f;

        #region Render passes
        private enum Passes
        {
            BlurAlphaWeighted,
            BoxBlur,
            DilateFgCocFromColor,
            DilateFgCoc,
            CaptureCocExplicit,
            VisualizeCocExplicit,
            CocPrefilter,
            CircleBlur,
            CircleBlurWithDilatedFg,
            CircleBlurLowQuality,
            CircleBlowLowQualityWithDilatedFg,
            MergeExplicit,
            ShapeLowQuality,
            ShapeLowQualityDilateFg,
            ShapeLowQualityMerge,
            ShapeLowQualityMergeDilateFg,
            ShapeMediumQuality,
            ShapeMediumQualityDilateFg,
            ShapeMediumQualityMerge,
            ShapeMediumQualityMergeDilateFg,
            ShapeHighQuality,
            ShapeHighQualityDilateFg,
            ShapeHighQualityMerge,
            ShapeHighQualityMergeDilateFg
        }

        private enum MedianPasses
        {
            Median3,
            Median3X3
        }

        private enum BokehTexturesPasses
        {
            Apply,
            Collect
        }
        #endregion

        public enum TweakMode
        {
            Range,
            Explicit
        }

        public enum ApertureShape
        {
            Circular,
            Hexagonal,
            Octogonal
        }

        public enum QualityPreset
        {
            Low,
            Medium,
            High
        }

        public enum FilterQuality
        {
            None,
            Normal,
            High
        }

        #region Settings
        [Serializable]
        public struct GlobalSettings
        {
            [Tooltip("Allows to view where the blur will be applied. Yellow for near blur, blue for far blur.")]
            public bool visualizeFocus;

            [Tooltip("Setup mode. Use \"Advanced\" if you need more control on blur settings and/or want to use a bokeh texture. \"Explicit\" is the same as \"Advanced\" but makes use of \"Near Plane\" and \"Far Plane\" values instead of \"F-Stop\".")]
            public TweakMode tweakMode;

            [Tooltip("Quality presets. Use \"Custom\" for more advanced settings.")]
            public QualityPreset filteringQuality;

            [Tooltip("\"Circular\" is the fastest, followed by \"Hexagonal\" and \"Octogonal\".")]
            public ApertureShape apertureShape;

            [Range(0f, 179f), Tooltip("Rotates the aperture when working with \"Hexagonal\" and \"Ortogonal\".")]
            public float apertureOrientation;

            public static GlobalSettings defaultSettings
            {
                get
                {
                    return new GlobalSettings
                    {
                        visualizeFocus = false,
                        tweakMode = TweakMode.Range,
                        filteringQuality = QualityPreset.High,
                        apertureShape = ApertureShape.Circular,
                        apertureOrientation = 0f
                    };
                }
            }
        }

        [Serializable]
        public struct QualitySettings
        {
            [Tooltip("Enable this to get smooth bokeh.")]
            public bool prefilterBlur;

            [Tooltip("Applies a median filter for even smoother bokeh.")]
            public FilterQuality medianFilter;

            [Tooltip("Dilates near blur over in focus area.")]
            public bool dilateNearBlur;

            public static QualitySettings[] presetQualitySettings =
            {
                // Low
                new QualitySettings
                {
                    prefilterBlur = false,
                    medianFilter = FilterQuality.None,
                    dilateNearBlur = false
                },

                // Medium
                new QualitySettings
                {
                    prefilterBlur = true,
                    medianFilter = FilterQuality.Normal,
                    dilateNearBlur = false
                },

                // High
                new QualitySettings
                {
                    prefilterBlur = true,
                    medianFilter = FilterQuality.High,
                    dilateNearBlur = true
                }
            };
        }

        [Serializable]
        public struct FocusSettings
        {
            [Tooltip("Auto-focus on a selected transform.")]
            public Transform transform;

            [Min(0f), Tooltip("Focus distance (in world units).")]
            public float focusPlane;

            [Min(0.1f), Tooltip("Focus range (in world units). The focus plane is located in the center of the range.")]
            public float range;

            [Min(0f), Tooltip("Near focus distance (in world units).")]
            public float nearPlane;

            [Min(0f), Tooltip("Near blur falloff (in world units).")]
            public float nearFalloff;

            [Min(0f), Tooltip("Far focus distance (in world units).")]
            public float farPlane;

            [Min(0f), Tooltip("Far blur falloff (in world units).")]
            public float farFalloff;

            [Range(0f, kMaxBlur), Tooltip("Maximum blur radius for the near plane.")]
            public float nearBlurRadius;

            [Range(0f, kMaxBlur), Tooltip("Maximum blur radius for the far plane.")]
            public float farBlurRadius;

            public static FocusSettings defaultSettings
            {
                get
                {
                    return new FocusSettings
                    {
                        transform = null,
                        focusPlane = 20f,
                        range = 35f,
                        nearPlane = 2.5f,
                        nearFalloff = 15f,
                        farPlane = 37.5f,
                        farFalloff = 50f,
                        nearBlurRadius = 15f,
                        farBlurRadius = 20f
                    };
                }
            }
        }

        [Serializable]
        public struct BokehTextureSettings
        {
            [Tooltip("Adding a texture to this field will enable the use of \"Bokeh Textures\". Use with care. This feature is only available on Shader Model 5 compatible-hardware and performance scale with the amount of bokeh.")]
            public Texture2D texture;

            [Range(0.01f, 10f), Tooltip("Maximum size of bokeh textures on screen.")]
            public float scale;

            [Range(0.01f, 100f), Tooltip("Bokeh brightness.")]
            public float intensity;

            [Range(0.01f, 5f), Tooltip("Controls the amount of bokeh textures. Lower values mean more bokeh splats.")]
            public float threshold;

            [Range(0.01f, 1f), Tooltip("Controls the spawn conditions. Lower values mean more visible bokeh.")]
            public float spawnHeuristic;

            public static BokehTextureSettings defaultSettings
            {
                get
                {
                    return new BokehTextureSettings
                    {
                        texture = null,
                        scale = 1f,
                        intensity = 50f,
                        threshold = 2f,
                        spawnHeuristic = 0.15f
                    };
                }
            }
        }
        #endregion

        public GlobalSettings settings = GlobalSettings.defaultSettings;
        public FocusSettings focus = FocusSettings.defaultSettings;
        public BokehTextureSettings bokehTexture = BokehTextureSettings.defaultSettings;

        [SerializeField]
        private Shader m_FilmicDepthOfFieldShader;

        public Shader filmicDepthOfFieldShader
        {
            get
            {
                if (m_FilmicDepthOfFieldShader == null)
                    m_FilmicDepthOfFieldShader = Shader.Find("Hidden/DepthOfField/DepthOfField");

                return m_FilmicDepthOfFieldShader;
            }
        }

        [SerializeField]
        private Shader m_MedianFilterShader;

        public Shader medianFilterShader
        {
            get
            {
                if (m_MedianFilterShader == null)
                    m_MedianFilterShader = Shader.Find("Hidden/DepthOfField/MedianFilter");

                return m_MedianFilterShader;
            }
        }

        [SerializeField]
        private Shader m_TextureBokehShader;

        public Shader textureBokehShader
        {
            get
            {
                if (m_TextureBokehShader == null)
                    m_TextureBokehShader = Shader.Find("Hidden/DepthOfField/BokehSplatting");

                return m_TextureBokehShader;
            }
        }

        private RenderTextureUtility m_RTU = new RenderTextureUtility();

        private Material m_FilmicDepthOfFieldMaterial;

        public Material filmicDepthOfFieldMaterial
        {
            get
            {
                if (m_FilmicDepthOfFieldMaterial == null)
                    m_FilmicDepthOfFieldMaterial = ImageEffectHelper.CheckShaderAndCreateMaterial(filmicDepthOfFieldShader);

                return m_FilmicDepthOfFieldMaterial;
            }
        }

        private Material m_MedianFilterMaterial;

        public Material medianFilterMaterial
        {
            get
            {
                if (m_MedianFilterMaterial == null)
                    m_MedianFilterMaterial = ImageEffectHelper.CheckShaderAndCreateMaterial(medianFilterShader);

                return m_MedianFilterMaterial;
            }
        }

        private Material m_TextureBokehMaterial;

        public Material textureBokehMaterial
        {
            get
            {
                if (m_TextureBokehMaterial == null)
                    m_TextureBokehMaterial = ImageEffectHelper.CheckShaderAndCreateMaterial(textureBokehShader);

                return m_TextureBokehMaterial;
            }
        }

        private ComputeBuffer m_ComputeBufferDrawArgs;

        public ComputeBuffer computeBufferDrawArgs
        {
            get
            {
                if (m_ComputeBufferDrawArgs == null)
                {
#if UNITY_5_4_OR_NEWER
                    m_ComputeBufferDrawArgs = new ComputeBuffer(1, 16, ComputeBufferType.IndirectArguments);
#else
                    m_ComputeBufferDrawArgs = new ComputeBuffer(1, 16, ComputeBufferType.DrawIndirect);
#endif
                    m_ComputeBufferDrawArgs.SetData(new[] {0, 1, 0, 0});
                }

                return m_ComputeBufferDrawArgs;
            }
        }

        private ComputeBuffer m_ComputeBufferPoints;

        public ComputeBuffer computeBufferPoints
        {
            get
            {
                if (m_ComputeBufferPoints == null)
                    m_ComputeBufferPoints = new ComputeBuffer(90000, 12 + 16, ComputeBufferType.Append);

                return m_ComputeBufferPoints;
            }
        }

        private QualitySettings m_CurrentQualitySettings;
        private float m_LastApertureOrientation;
        private Vector4 m_OctogonalBokehDirection1;
        private Vector4 m_OctogonalBokehDirection2;
        private Vector4 m_OctogonalBokehDirection3;
        private Vector4 m_OctogonalBokehDirection4;
        private Vector4 m_HexagonalBokehDirection1;
        private Vector4 m_HexagonalBokehDirection2;
        private Vector4 m_HexagonalBokehDirection3;

        private int m_BlurParams;
        private int m_BlurCoe;
        private int m_Offsets;
        private int m_BlurredColor;
        private int m_SpawnHeuristic;
        private int m_BokehParams;
        private int m_Convolved_TexelSize;
        private int m_SecondTex;
        private int m_ThirdTex;
        private int m_MainTex;
        private int m_Screen;

        private void Awake()
        {
            m_BlurParams = Shader.PropertyToID("_BlurParams");
            m_BlurCoe = Shader.PropertyToID("_BlurCoe");
            m_Offsets = Shader.PropertyToID("_Offsets");
            m_BlurredColor = Shader.PropertyToID("_BlurredColor");
            m_SpawnHeuristic = Shader.PropertyToID("_SpawnHeuristic");
            m_BokehParams = Shader.PropertyToID("_BokehParams");
            m_Convolved_TexelSize = Shader.PropertyToID("_Convolved_TexelSize");
            m_SecondTex = Shader.PropertyToID("_SecondTex");
            m_ThirdTex = Shader.PropertyToID("_ThirdTex");
            m_MainTex = Shader.PropertyToID("_MainTex");
            m_Screen = Shader.PropertyToID("_Screen");
        }

        private void OnEnable()
        {
            if (!ImageEffectHelper.IsSupported(filmicDepthOfFieldShader, true, true, this) || !ImageEffectHelper.IsSupported(medianFilterShader, true, true, this))
            {
                enabled = false;
                return;
            }

            if (ImageEffectHelper.supportsDX11 && !ImageEffectHelper.IsSupported(textureBokehShader, true, true, this))
            {
                enabled = false;
                return;
            }

            ComputeBlurDirections(true);
            GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
        }

        private void OnDisable()
        {
            ReleaseComputeResources();

            if (m_FilmicDepthOfFieldMaterial != null)
                DestroyImmediate(m_FilmicDepthOfFieldMaterial);

            if (m_TextureBokehMaterial != null)
                DestroyImmediate(m_TextureBokehMaterial);

            if (m_MedianFilterMaterial != null)
                DestroyImmediate(m_MedianFilterMaterial);

            m_FilmicDepthOfFieldMaterial = null;
            m_TextureBokehMaterial = null;
            m_MedianFilterMaterial = null;

            m_RTU.ReleaseAllTemporaryRenderTextures();
        }

        //-------------------------------------------------------------------//
        // Main entry point                                                  //
        //-------------------------------------------------------------------//
        [ImageEffectOpaque]
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (medianFilterMaterial == null || filmicDepthOfFieldMaterial == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            if (settings.visualizeFocus)
            {
                Vector4 blurrinessParam;
                Vector4 blurrinessCoe;
                ComputeCocParameters(out blurrinessParam, out blurrinessCoe);
                filmicDepthOfFieldMaterial.SetVector(m_BlurParams, blurrinessParam);
                filmicDepthOfFieldMaterial.SetVector(m_BlurCoe, blurrinessCoe);
                Graphics.Blit(null, destination, filmicDepthOfFieldMaterial, (int)Passes.VisualizeCocExplicit);
            }
            else
            {
                DoDepthOfField(source, destination);
            }

            m_RTU.ReleaseAllTemporaryRenderTextures();
        }

        private void DoDepthOfField(RenderTexture source, RenderTexture destination)
        {
            m_CurrentQualitySettings = QualitySettings.presetQualitySettings[(int)settings.filteringQuality];

            float radiusAdjustement = source.height / 720f;

            float textureBokehScale = radiusAdjustement;
            float textureBokehMaxRadius = Mathf.Max(focus.nearBlurRadius, focus.farBlurRadius) * textureBokehScale * 0.75f;

            float nearBlurRadius = focus.nearBlurRadius * radiusAdjustement;
            float farBlurRadius = focus.farBlurRadius * radiusAdjustement;
            float maxBlurRadius = Mathf.Max(nearBlurRadius, farBlurRadius);
            switch (settings.apertureShape)
            {
                case ApertureShape.Hexagonal:
                    maxBlurRadius *= 1.2f;
                    break;
                case ApertureShape.Octogonal:
                    maxBlurRadius *= 1.15f;
                    break;
            }

            if (maxBlurRadius < 0.5f)
            {
                Graphics.Blit(source, destination);
                return;
            }

            // Quarter resolution
            int rtW = source.width / 2;
            int rtH = source.height / 2;
            var blurrinessCoe = new Vector4(nearBlurRadius * 0.5f, farBlurRadius * 0.5f, 0f, 0f);
            var colorAndCoc = m_RTU.GetTemporaryRenderTexture(rtW, rtH);
            var colorAndCoc2 = m_RTU.GetTemporaryRenderTexture(rtW, rtH);

            // Downsample to Color + COC buffer
            Vector4 cocParam;
            Vector4 cocCoe;
            ComputeCocParameters(out cocParam, out cocCoe);
            filmicDepthOfFieldMaterial.SetVector(m_BlurParams, cocParam);
            filmicDepthOfFieldMaterial.SetVector(m_BlurCoe, cocCoe);
            Graphics.Blit(source, colorAndCoc2, filmicDepthOfFieldMaterial, (int)Passes.CaptureCocExplicit);
            var src = colorAndCoc2;
            var dst = colorAndCoc;

            // Collect texture bokeh candidates and replace with a darker pixel
            if (shouldPerformBokeh)
            {
                // Blur a bit so we can do a frequency check
                var blurred = m_RTU.GetTemporaryRenderTexture(rtW, rtH);
                Graphics.Blit(src, blurred, filmicDepthOfFieldMaterial, (int)Passes.BoxBlur);
                filmicDepthOfFieldMaterial.SetVector(m_Offsets, new Vector4(0f, 1.5f, 0f, 1.5f));
                Graphics.Blit(blurred, dst, filmicDepthOfFieldMaterial, (int)Passes.BlurAlphaWeighted);
                filmicDepthOfFieldMaterial.SetVector(m_Offsets, new Vector4(1.5f, 0f, 0f, 1.5f));
                Graphics.Blit(dst, blurred, filmicDepthOfFieldMaterial, (int)Passes.BlurAlphaWeighted);

                // Collect texture bokeh candidates and replace with a darker pixel
                textureBokehMaterial.SetTexture(m_BlurredColor, blurred);
                textureBokehMaterial.SetFloat(m_SpawnHeuristic, bokehTexture.spawnHeuristic);
                textureBokehMaterial.SetVector(m_BokehParams, new Vector4(bokehTexture.scale * textureBokehScale, bokehTexture.intensity, bokehTexture.threshold, textureBokehMaxRadius));
                Graphics.SetRandomWriteTarget(1, computeBufferPoints);
                Graphics.Blit(src, dst, textureBokehMaterial, (int)BokehTexturesPasses.Collect);
                Graphics.ClearRandomWriteTargets();
                SwapRenderTexture(ref src, ref dst);
                m_RTU.ReleaseTemporaryRenderTexture(blurred);
            }

            filmicDepthOfFieldMaterial.SetVector(m_BlurParams, cocParam);
            filmicDepthOfFieldMaterial.SetVector(m_BlurCoe, blurrinessCoe);

            // Dilate near blur factor
            RenderTexture blurredFgCoc = null;
            if (m_CurrentQualitySettings.dilateNearBlur)
            {
                var blurredFgCoc2 = m_RTU.GetTemporaryRenderTexture(rtW, rtH, 0, RenderTextureFormat.RGHalf);
                blurredFgCoc = m_RTU.GetTemporaryRenderTexture(rtW, rtH, 0, RenderTextureFormat.RGHalf);
                filmicDepthOfFieldMaterial.SetVector(m_Offsets, new Vector4(0f, nearBlurRadius * 0.75f, 0f, 0f));
                Graphics.Blit(src, blurredFgCoc2, filmicDepthOfFieldMaterial, (int)Passes.DilateFgCocFromColor);
                filmicDepthOfFieldMaterial.SetVector(m_Offsets, new Vector4(nearBlurRadius * 0.75f, 0f, 0f, 0f));
                Graphics.Blit(blurredFgCoc2, blurredFgCoc, filmicDepthOfFieldMaterial, (int)Passes.DilateFgCoc);
                m_RTU.ReleaseTemporaryRenderTexture(blurredFgCoc2);
                blurredFgCoc.filterMode = FilterMode.Point;
            }

            // Blur downsampled color to fill the gap between samples
            if (m_CurrentQualitySettings.prefilterBlur)
            {
                Graphics.Blit(src, dst, filmicDepthOfFieldMaterial, (int)Passes.CocPrefilter);
                SwapRenderTexture(ref src, ref dst);
            }

            // Apply blur : Circle / Hexagonal or Octagonal (blur will create bokeh if bright pixel where not removed by "m_UseBokehTexture")
            switch (settings.apertureShape)
            {
                case ApertureShape.Circular:
                    DoCircularBlur(blurredFgCoc, ref src, ref dst, maxBlurRadius);
                    break;
                case ApertureShape.Hexagonal:
                    DoHexagonalBlur(blurredFgCoc, ref src, ref dst, maxBlurRadius);
                    break;
                case ApertureShape.Octogonal:
                    DoOctogonalBlur(blurredFgCoc, ref src, ref dst, maxBlurRadius);
                    break;
            }

            // Smooth result
            switch (m_CurrentQualitySettings.medianFilter)
            {
                case FilterQuality.Normal:
                {
                    medianFilterMaterial.SetVector(m_Offsets, new Vector4(1f, 0f, 0f, 0f));
                    Graphics.Blit(src, dst, medianFilterMaterial, (int)MedianPasses.Median3);
                    SwapRenderTexture(ref src, ref dst);
                    medianFilterMaterial.SetVector(m_Offsets, new Vector4(0f, 1f, 0f, 0f));
                    Graphics.Blit(src, dst, medianFilterMaterial, (int)MedianPasses.Median3);
                    SwapRenderTexture(ref src, ref dst);
                    break;
                }
                case FilterQuality.High:
                {
                    Graphics.Blit(src, dst, medianFilterMaterial, (int)MedianPasses.Median3X3);
                    SwapRenderTexture(ref src, ref dst);
                    break;
                }
            }

            // Merge to full resolution (with boost) + upsampling (linear or bicubic)
            filmicDepthOfFieldMaterial.SetVector(m_BlurCoe, blurrinessCoe);
            filmicDepthOfFieldMaterial.SetVector(m_Convolved_TexelSize, new Vector4(src.width, src.height, 1f / src.width, 1f / src.height));
            filmicDepthOfFieldMaterial.SetTexture(m_SecondTex, src);
            int mergePass = (int)Passes.MergeExplicit;

            // Apply texture bokeh
            if (shouldPerformBokeh)
            {
                var tmp = m_RTU.GetTemporaryRenderTexture(source.height, source.width, 0, source.format);
                Graphics.Blit(source, tmp, filmicDepthOfFieldMaterial, mergePass);

                Graphics.SetRenderTarget(tmp);
                ComputeBuffer.CopyCount(computeBufferPoints, computeBufferDrawArgs, 0);
                textureBokehMaterial.SetBuffer("pointBuffer", computeBufferPoints);
                textureBokehMaterial.SetTexture(m_MainTex, bokehTexture.texture);
                textureBokehMaterial.SetVector(m_Screen, new Vector3(1f / (1f * source.width), 1f / (1f * source.height), textureBokehMaxRadius));
                textureBokehMaterial.SetPass((int)BokehTexturesPasses.Apply);
                Graphics.DrawProceduralIndirectNow(MeshTopology.Points, computeBufferDrawArgs, 0);
                Graphics.Blit(tmp, destination); // Hackaround for DX11 flipfun (OPTIMIZEME)
            }
            else
            {
                Graphics.Blit(source, destination, filmicDepthOfFieldMaterial, mergePass);
            }
        }

        //-------------------------------------------------------------------//
        // Blurs                                                             //
        //-------------------------------------------------------------------//
        private void DoHexagonalBlur(RenderTexture blurredFgCoc, ref RenderTexture src, ref RenderTexture dst, float maxRadius)
        {
            ComputeBlurDirections(false);

            int blurPass;
            int blurPassMerge;
            GetDirectionalBlurPassesFromRadius(blurredFgCoc, maxRadius, out blurPass, out blurPassMerge);
            filmicDepthOfFieldMaterial.SetTexture(m_SecondTex, blurredFgCoc);
            var tmp = m_RTU.GetTemporaryRenderTexture(src.width, src.height, 0, src.format);

            filmicDepthOfFieldMaterial.SetVector(m_Offsets, m_HexagonalBokehDirection1);
            Graphics.Blit(src, tmp, filmicDepthOfFieldMaterial, blurPass);

            filmicDepthOfFieldMaterial.SetVector(m_Offsets, m_HexagonalBokehDirection2);
            Graphics.Blit(tmp, src, filmicDepthOfFieldMaterial, blurPass);

            filmicDepthOfFieldMaterial.SetVector(m_Offsets, m_HexagonalBokehDirection3);
            filmicDepthOfFieldMaterial.SetTexture(m_ThirdTex, src);
            Graphics.Blit(tmp, dst, filmicDepthOfFieldMaterial, blurPassMerge);
            m_RTU.ReleaseTemporaryRenderTexture(tmp);
            SwapRenderTexture(ref src, ref dst);
        }

        private void DoOctogonalBlur(RenderTexture blurredFgCoc, ref RenderTexture src, ref RenderTexture dst, float maxRadius)
        {
            ComputeBlurDirections(false);

            int blurPass;
            int blurPassMerge;
            GetDirectionalBlurPassesFromRadius(blurredFgCoc, maxRadius, out blurPass, out blurPassMerge);
            filmicDepthOfFieldMaterial.SetTexture(m_SecondTex, blurredFgCoc);
            var tmp = m_RTU.GetTemporaryRenderTexture(src.width, src.height, 0, src.format);

            filmicDepthOfFieldMaterial.SetVector(m_Offsets, m_OctogonalBokehDirection1);
            Graphics.Blit(src, tmp, filmicDepthOfFieldMaterial, blurPass);

            filmicDepthOfFieldMaterial.SetVector(m_Offsets, m_OctogonalBokehDirection2);
            Graphics.Blit(tmp, dst, filmicDepthOfFieldMaterial, blurPass);

            filmicDepthOfFieldMaterial.SetVector(m_Offsets, m_OctogonalBokehDirection3);
            Graphics.Blit(src, tmp, filmicDepthOfFieldMaterial, blurPass);

            filmicDepthOfFieldMaterial.SetVector(m_Offsets, m_OctogonalBokehDirection4);
            filmicDepthOfFieldMaterial.SetTexture(m_ThirdTex, dst);
            Graphics.Blit(tmp, src, filmicDepthOfFieldMaterial, blurPassMerge);
            m_RTU.ReleaseTemporaryRenderTexture(tmp);
        }

        private void DoCircularBlur(RenderTexture blurredFgCoc, ref RenderTexture src, ref RenderTexture dst, float maxRadius)
        {
            int bokehPass;

            if (blurredFgCoc != null)
            {
                filmicDepthOfFieldMaterial.SetTexture(m_SecondTex, blurredFgCoc);
                bokehPass = (maxRadius > 10f) ? (int)Passes.CircleBlurWithDilatedFg : (int)Passes.CircleBlowLowQualityWithDilatedFg;
            }
            else
            {
                bokehPass = (maxRadius > 10f) ? (int)Passes.CircleBlur : (int)Passes.CircleBlurLowQuality;
            }

            Graphics.Blit(src, dst, filmicDepthOfFieldMaterial, bokehPass);
            SwapRenderTexture(ref src, ref dst);
        }

        //-------------------------------------------------------------------//
        // Helpers                                                           //
        //-------------------------------------------------------------------//
        private void ComputeCocParameters(out Vector4 blurParams, out Vector4 blurCoe)
        {
            var sceneCamera = GetComponent<Camera>();

            float focusDistance;
            float nearFalloff = focus.nearFalloff * 2f;
            float farFalloff = focus.farFalloff * 2f;
            float nearPlane = focus.nearPlane;
            float farPlane = focus.farPlane;

            if (settings.tweakMode == TweakMode.Range)
            {
                if (focus.transform != null)
                    focusDistance = sceneCamera.WorldToViewportPoint(focus.transform.position).z;
                else
                    focusDistance = focus.focusPlane;

                float s = focus.range * 0.5f;
                nearPlane = focusDistance - s;
                farPlane = focusDistance + s;
            }

            nearPlane -= (nearFalloff * 0.5f);
            farPlane += (farFalloff * 0.5f);
            focusDistance = (nearPlane + farPlane) * 0.5f;

            float focusDistance01 = focusDistance / sceneCamera.farClipPlane;
            float nearDistance01 = nearPlane / sceneCamera.farClipPlane;
            float farDistance01 = farPlane / sceneCamera.farClipPlane;

            var dof = farPlane - nearPlane;
            var dof01 = farDistance01 - nearDistance01;
            var nearFalloff01 = nearFalloff / dof;
            var farFalloff01 = farFalloff / dof;
            float nearFocusRange01 = (1f - nearFalloff01) * (dof01 * 0.5f);
            float farFocusRange01 = (1f - farFalloff01) * (dof01 * 0.5f);

            if (focusDistance01 <= nearDistance01)
                focusDistance01 = nearDistance01 + 1e-6f;
            if (focusDistance01 >= farDistance01)
                focusDistance01 = farDistance01 - 1e-6f;

            if ((focusDistance01 - nearFocusRange01) <= nearDistance01)
                nearFocusRange01 = focusDistance01 - nearDistance01 - 1e-6f;
            if ((focusDistance01 + farFocusRange01) >= farDistance01)
                farFocusRange01 = farDistance01 - focusDistance01 - 1e-6f;

            float a1 = 1f / (nearDistance01 - focusDistance01 + nearFocusRange01);
            float a2 = 1f / (farDistance01 - focusDistance01 - farFocusRange01);
            float b1 = 1f - a1 * nearDistance01;
            float b2 = 1f - a2 * farDistance01;
            const float c1 = -1f;
            const float c2 = 1f;
            blurParams = new Vector4(c1 * a1, c1 * b1, c2 * a2, c2 * b2);
            blurCoe = new Vector4(0f, 0f, (b2 - b1) / (a1 - a2), 0f);

            // Save values so we can switch from one tweak mode to the other on the fly
            focus.nearPlane = nearPlane + (nearFalloff * 0.5f);
            focus.farPlane = farPlane - (farFalloff * 0.5f);
            focus.focusPlane = (focus.nearPlane + focus.farPlane) * 0.5f;
            focus.range = focus.farPlane - focus.nearPlane;
        }

        private void ReleaseComputeResources()
        {
            if (m_ComputeBufferDrawArgs != null)
                m_ComputeBufferDrawArgs.Release();

            if (m_ComputeBufferPoints != null)
                m_ComputeBufferPoints.Release();

            m_ComputeBufferDrawArgs = null;
            m_ComputeBufferPoints = null;
        }

        private void ComputeBlurDirections(bool force)
        {
            if (!force && Math.Abs(m_LastApertureOrientation - settings.apertureOrientation) < float.Epsilon)
                return;

            m_LastApertureOrientation = settings.apertureOrientation;

            float rotationRadian = settings.apertureOrientation * Mathf.Deg2Rad;
            float cosinus = Mathf.Cos(rotationRadian);
            float sinus = Mathf.Sin(rotationRadian);

            m_OctogonalBokehDirection1 = new Vector4(0.5f, 0f, 0f, 0f);
            m_OctogonalBokehDirection2 = new Vector4(0f, 0.5f, 1f, 0f);
            m_OctogonalBokehDirection3 = new Vector4(-0.353553f, 0.353553f, 1f, 0f);
            m_OctogonalBokehDirection4 = new Vector4(0.353553f, 0.353553f, 1f, 0f);

            m_HexagonalBokehDirection1 = new Vector4(0.5f, 0f, 0f, 0f);
            m_HexagonalBokehDirection2 = new Vector4(0.25f, 0.433013f, 1f, 0f);
            m_HexagonalBokehDirection3 = new Vector4(0.25f, -0.433013f, 1f, 0f);

            if (rotationRadian > float.Epsilon)
            {
                Rotate2D(ref m_OctogonalBokehDirection1, cosinus, sinus);
                Rotate2D(ref m_OctogonalBokehDirection2, cosinus, sinus);
                Rotate2D(ref m_OctogonalBokehDirection3, cosinus, sinus);
                Rotate2D(ref m_OctogonalBokehDirection4, cosinus, sinus);
                Rotate2D(ref m_HexagonalBokehDirection1, cosinus, sinus);
                Rotate2D(ref m_HexagonalBokehDirection2, cosinus, sinus);
                Rotate2D(ref m_HexagonalBokehDirection3, cosinus, sinus);
            }
        }

        private bool shouldPerformBokeh
        {
            get { return ImageEffectHelper.supportsDX11 && bokehTexture.texture != null && textureBokehMaterial; }
        }

        private static void Rotate2D(ref Vector4 direction, float cosinus, float sinus)
        {
            var source = direction;
            direction.x = source.x * cosinus - source.y * sinus;
            direction.y = source.x * sinus + source.y * cosinus;
        }

        private static void SwapRenderTexture(ref RenderTexture src, ref RenderTexture dst)
        {
            RenderTexture tmp = dst;
            dst = src;
            src = tmp;
        }

        private static void GetDirectionalBlurPassesFromRadius(RenderTexture blurredFgCoc, float maxRadius, out int blurPass, out int blurAndMergePass)
        {
            if (blurredFgCoc == null)
            {
                if (maxRadius > 10f)
                {
                    blurPass = (int)Passes.ShapeHighQuality;
                    blurAndMergePass = (int)Passes.ShapeHighQualityMerge;
                }
                else if (maxRadius > 5f)
                {
                    blurPass = (int)Passes.ShapeMediumQuality;
                    blurAndMergePass = (int)Passes.ShapeMediumQualityMerge;
                }
                else
                {
                    blurPass = (int)Passes.ShapeLowQuality;
                    blurAndMergePass = (int)Passes.ShapeLowQualityMerge;
                }
            }
            else
            {
                if (maxRadius > 10f)
                {
                    blurPass = (int)Passes.ShapeHighQualityDilateFg;
                    blurAndMergePass = (int)Passes.ShapeHighQualityMergeDilateFg;
                }
                else if (maxRadius > 5f)
                {
                    blurPass = (int)Passes.ShapeMediumQualityDilateFg;
                    blurAndMergePass = (int)Passes.ShapeMediumQualityMergeDilateFg;
                }
                else
                {
                    blurPass = (int)Passes.ShapeLowQualityDilateFg;
                    blurAndMergePass = (int)Passes.ShapeLowQualityMergeDilateFg;
                }
            }
        }
    }
}
