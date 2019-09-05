using System;
using UnityEngine;

using UnityEngine.Rendering;

namespace UnityStandardAssets.CinematicEffects
{
    [ExecuteInEditMode]
#if UNITY_5_4_OR_NEWER
    [ImageEffectAllowedInSceneView]
#endif
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Image Effects/Cinematic/Screen Space Reflections")]
    public class ScreenSpaceReflection : MonoBehaviour
    {
        public enum SSRResolution
        {
            High = 0,
            Low = 2
        }

        public enum SSRReflectionBlendType
        {
            PhysicallyBased,
            Additive
        }

        [Serializable]
        public struct SSRSettings
        {
            [AttributeUsage(AttributeTargets.Field)]
            public class LayoutAttribute : PropertyAttribute
            {
            }

            [Layout]
            public ReflectionSettings reflectionSettings;

            [Layout]
            public IntensitySettings intensitySettings;

            [Layout]
            public ScreenEdgeMask screenEdgeMask;

            private static readonly SSRSettings s_Default = new SSRSettings
            {
                reflectionSettings = new ReflectionSettings
                {
                    blendType = SSRReflectionBlendType.PhysicallyBased,
                    reflectionQuality = SSRResolution.High,
                    maxDistance = 100.0f,
                    iterationCount = 256,
                    stepSize = 3,
                    widthModifier = 0.5f,
                    reflectionBlur = 1.0f,
                    reflectBackfaces = true
                },

                intensitySettings = new IntensitySettings
                {
                    reflectionMultiplier = 1.0f,
                    fadeDistance = 100.0f,

                    fresnelFade = 1.0f,
                    fresnelFadePower = 1.0f,
                },

                screenEdgeMask = new ScreenEdgeMask
                {
                    intensity = 0.03f
                }
            };

            public static SSRSettings defaultSettings
            {
                get
                {
                    return s_Default;
                }
            }
        }

        [Serializable]
        public struct IntensitySettings
        {
            [Tooltip("Nonphysical multiplier for the SSR reflections. 1.0 is physically based.")]
            [Range(0.0f, 2.0f)]
            public float reflectionMultiplier;

            [Tooltip("How far away from the maxDistance to begin fading SSR.")]
            [Range(0.0f, 1000.0f)]
            public float fadeDistance;

            [Tooltip("Amplify Fresnel fade out. Increase if floor reflections look good close to the surface and bad farther 'under' the floor.")]
            [Range(0.0f, 1.0f)]
            public float fresnelFade;

            [Tooltip("Higher values correspond to a faster Fresnel fade as the reflection changes from the grazing angle.")]
            [Range(0.1f, 10.0f)]
            public float fresnelFadePower;
        }

        [Serializable]
        public struct ReflectionSettings
        {
            // When enabled, we just add our reflections on top of the existing ones. This is physically incorrect, but several
            // popular demos and games have taken this approach, and it does hide some artifacts.
            [Tooltip("How the reflections are blended into the render.")]
            public SSRReflectionBlendType blendType;

            [Tooltip("Half resolution SSRR is much faster, but less accurate.")]
            public SSRResolution reflectionQuality;

            [Tooltip("Maximum reflection distance in world units.")]
            [Range(0.1f, 300.0f)]
            public float maxDistance;

            /// REFLECTIONS
            [Tooltip("Max raytracing length.")]
            [Range(16, 1024)]
            public int iterationCount;

            [Tooltip("Log base 2 of ray tracing coarse step size. Higher traces farther, lower gives better quality silhouettes.")]
            [Range(1, 16)]
            public int stepSize;

            [Tooltip("Typical thickness of columns, walls, furniture, and other objects that reflection rays might pass behind.")]
            [Range(0.01f, 10.0f)]
            public float widthModifier;

            [Tooltip("Blurriness of reflections.")]
            [Range(0.1f, 8.0f)]
            public float reflectionBlur;

            [Tooltip("Enable for a performance gain in scenes where most glossy objects are horizontal, like floors, water, and tables. Leave on for scenes with glossy vertical objects.")]
            public bool reflectBackfaces;
        }

        [Serializable]
        public struct ScreenEdgeMask
        {
            [Tooltip("Higher = fade out SSRR near the edge of the screen so that reflections don't pop under camera motion.")]
            [Range(0.0f, 1.0f)]
            public float intensity;
        }


        [SerializeField]
        public SSRSettings settings = SSRSettings.defaultSettings;

        ///////////// Unexposed Variables //////////////////

        [Tooltip("Enable to limit the effect a few bright pixels can have on rougher surfaces")]
        private bool highlightSuppression = false;

        [Tooltip("Enable to allow rays to pass behind objects. This can lead to more screen-space reflections, but the reflections are more likely to be wrong.")]
        private bool traceBehindObjects = true;

        [Tooltip("Enable to force more surfaces to use reflection probes if you see streaks on the sides of objects or bad reflections of their backs.")]
        private bool treatBackfaceHitAsMiss = false;

        [Tooltip("Drastically improves reflection reconstruction quality at the expense of some performance.")]
        private  bool bilateralUpsample = true;

        ///////////// Unexposed Variables //////////////////

        [SerializeField]
        private Shader m_Shader;
        public Shader shader
        {
            get
            {
                if (m_Shader == null)
                    m_Shader = Shader.Find("Hidden/ScreenSpaceReflection");

                return m_Shader;
            }
        }

        private Material m_Material;
        public Material material
        {
            get
            {
                if (m_Material == null)
                    m_Material = ImageEffectHelper.CheckShaderAndCreateMaterial(shader);

                return m_Material;
            }
        }

        private Camera m_Camera;
        public Camera camera_
        {
            get
            {
                if (m_Camera == null)
                    m_Camera = GetComponent<Camera>();

                return m_Camera;
            }
        }

        private CommandBuffer m_CommandBuffer;

        private static int kNormalAndRoughnessTexture;
        private static int kHitPointTexture;
        private static int[] kReflectionTextures;
        private static int kFilteredReflections;
        private static int kBlurTexture;
        private static int kFinalReflectionTexture;
        private static int kTempTexture;

        private static int kRayStepSize;
        private static int kAdditiveReflection;
        private static int kBilateralUpsampling;
        private static int kTreatBackfaceHitAsMiss;
        private static int kAllowBackwardsRays;
        private static int kTraceBehindObjects;
        private static int kMaxSteps;
        private static int kFullResolutionFiltering;
        private static int kHalfResolution;
        private static int kHighlightSuppression;
        private static int kPixelsPerMeterAtOneMeter;
        private static int kScreenEdgeFading;
        private static int kReflectionBlur;
        private static int kMaxRayTraceDistance;
        private static int kFadeDistance;
        private static int kLayerThickness;
        private static int kSSRMultiplier;
        private static int kFresnelFade;
        private static int kFresnelFadePower;
        private static int kReflectionBufferSize;
        private static int kScreenSize;
        private static int kInvScreenSize;
        private static int kProjInfo;
        private static int kCameraClipInfo;
        private static int kProjectToPixelMatrix;
        private static int kWorldToCameraMatrix;
        private static int kCameraToWorldMatrix;
        private static int kAxis;
        private static int kCurrentMipLevel;

        // Shader pass indices used by the effect
        private enum PassIndex
        {
            RayTraceStep = 0,
            CompositeFinal = 1,
            Blur = 2,
            CompositeSSR = 3,
            MinMipGeneration = 4,
            HitPointToReflections = 5,
            BilateralKeyPack = 6,
            BlitDepthAsCSZ = 7,
            PoissonBlur = 8,
        }

        private void Awake()
        {
            kReflectionTextures = new int[5];

            kNormalAndRoughnessTexture = Shader.PropertyToID("_NormalAndRoughnessTexture");
            kHitPointTexture = Shader.PropertyToID("_HitPointTexture");
            kReflectionTextures[0] = Shader.PropertyToID("_ReflectionTexture0");
            kReflectionTextures[1] = Shader.PropertyToID("_ReflectionTexture1");
            kReflectionTextures[2] = Shader.PropertyToID("_ReflectionTexture2");
            kReflectionTextures[3] = Shader.PropertyToID("_ReflectionTexture3");
            kReflectionTextures[4] = Shader.PropertyToID("_ReflectionTexture4");
            kBlurTexture = Shader.PropertyToID("_BlurTexture");
            kFilteredReflections = Shader.PropertyToID("_FilteredReflections");
            kFinalReflectionTexture = Shader.PropertyToID("_FinalReflectionTexture");
            kTempTexture = Shader.PropertyToID("_TempTexture");

            kRayStepSize = Shader.PropertyToID("_RayStepSize");
            kAdditiveReflection = Shader.PropertyToID("_AdditiveReflection");
            kBilateralUpsampling = Shader.PropertyToID("_BilateralUpsampling");
            kTreatBackfaceHitAsMiss = Shader.PropertyToID("_TreatBackfaceHitAsMiss");
            kAllowBackwardsRays = Shader.PropertyToID("_AllowBackwardsRays");
            kTraceBehindObjects = Shader.PropertyToID("_TraceBehindObjects");
            kMaxSteps = Shader.PropertyToID("_MaxSteps");
            kFullResolutionFiltering = Shader.PropertyToID("_FullResolutionFiltering");
            kHalfResolution = Shader.PropertyToID("_HalfResolution");
            kHighlightSuppression = Shader.PropertyToID("_HighlightSuppression");
            kPixelsPerMeterAtOneMeter = Shader.PropertyToID("_PixelsPerMeterAtOneMeter");
            kScreenEdgeFading = Shader.PropertyToID("_ScreenEdgeFading");
            kReflectionBlur = Shader.PropertyToID("_ReflectionBlur");
            kMaxRayTraceDistance = Shader.PropertyToID("_MaxRayTraceDistance");
            kFadeDistance = Shader.PropertyToID("_FadeDistance");
            kLayerThickness = Shader.PropertyToID("_LayerThickness");
            kSSRMultiplier = Shader.PropertyToID("_SSRMultiplier");
            kFresnelFade = Shader.PropertyToID("_FresnelFade");
            kFresnelFadePower = Shader.PropertyToID("_FresnelFadePower");
            kReflectionBufferSize = Shader.PropertyToID("_ReflectionBufferSize");
            kScreenSize = Shader.PropertyToID("_ScreenSize");
            kInvScreenSize = Shader.PropertyToID("_InvScreenSize");
            kProjInfo = Shader.PropertyToID("_ProjInfo");
            kCameraClipInfo = Shader.PropertyToID("_CameraClipInfo");
            kProjectToPixelMatrix = Shader.PropertyToID("_ProjectToPixelMatrix");
            kWorldToCameraMatrix = Shader.PropertyToID("_WorldToCameraMatrix");
            kCameraToWorldMatrix = Shader.PropertyToID("_CameraToWorldMatrix");
            kAxis = Shader.PropertyToID("_Axis");
            kCurrentMipLevel = Shader.PropertyToID("_CurrentMipLevel");
        }

        private void OnEnable()
        {
            if (!ImageEffectHelper.IsSupported(shader, false, true, this))
            {
                enabled = false;
                return;
            }

            camera_.depthTextureMode |= DepthTextureMode.Depth;
        }

        void OnDisable()
        {
            if (m_Material)
                DestroyImmediate(m_Material);

            m_Material = null;

            if (camera_ != null)
            {
                if (m_CommandBuffer != null)
                {
                    camera_.RemoveCommandBuffer(CameraEvent.AfterFinalPass, m_CommandBuffer);
                }

                m_CommandBuffer = null;
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (camera_ != null)
            {
                if (m_CommandBuffer != null)
                {
                    camera_.RemoveCommandBuffer(CameraEvent.AfterFinalPass, m_CommandBuffer);
                }

                m_CommandBuffer = null;
            }
        }
#endif

        // [ImageEffectOpaque]
        public void OnPreRender()
        {
            if (material == null)
            {
                return;
            }
            else if (Camera.current.actualRenderingPath != RenderingPath.DeferredShading)
            {
                return;
            }

            int downsampleAmount = (settings.reflectionSettings.reflectionQuality == SSRResolution.High) ? 1 : 2;

            var rtW = camera_.pixelWidth / downsampleAmount;
            var rtH = camera_.pixelHeight / downsampleAmount;

            float sWidth = camera_.pixelWidth;
            float sHeight = camera_.pixelHeight;

            float sx = sWidth / 2.0f;
            float sy = sHeight / 2.0f;

            const int maxMip = 5;

            RenderTextureFormat intermediateFormat = camera_.allowHDR ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;

            material.SetInt(kRayStepSize, settings.reflectionSettings.stepSize);
            material.SetInt(kAdditiveReflection, settings.reflectionSettings.blendType == SSRReflectionBlendType.Additive ? 1 : 0);
            material.SetInt(kBilateralUpsampling, bilateralUpsample ? 1 : 0);
            material.SetInt(kTreatBackfaceHitAsMiss, treatBackfaceHitAsMiss ? 1 : 0);
            material.SetInt(kAllowBackwardsRays, settings.reflectionSettings.reflectBackfaces ? 1 : 0);
            material.SetInt(kTraceBehindObjects, traceBehindObjects ? 1 : 0);
            material.SetInt(kMaxSteps, settings.reflectionSettings.iterationCount);
            material.SetInt(kFullResolutionFiltering, 0);
            material.SetInt(kHalfResolution, (settings.reflectionSettings.reflectionQuality != SSRResolution.High) ? 1 : 0);
            material.SetInt(kHighlightSuppression, highlightSuppression ? 1 : 0);

            /** The height in pixels of a 1m object if viewed from 1m away. */
            float pixelsPerMeterAtOneMeter = sWidth / (-2.0f * (float)(Math.Tan(camera_.fieldOfView / 180.0 * Math.PI * 0.5)));

            material.SetFloat(kPixelsPerMeterAtOneMeter, pixelsPerMeterAtOneMeter);
            material.SetFloat(kScreenEdgeFading, settings.screenEdgeMask.intensity);
            material.SetFloat(kReflectionBlur, settings.reflectionSettings.reflectionBlur);
            material.SetFloat(kMaxRayTraceDistance, settings.reflectionSettings.maxDistance);
            material.SetFloat(kFadeDistance, settings.intensitySettings.fadeDistance);
            material.SetFloat(kLayerThickness, settings.reflectionSettings.widthModifier);
            material.SetFloat(kSSRMultiplier, settings.intensitySettings.reflectionMultiplier);
            material.SetFloat(kFresnelFade, settings.intensitySettings.fresnelFade);
            material.SetFloat(kFresnelFadePower, settings.intensitySettings.fresnelFadePower);

            Matrix4x4 P = camera_.projectionMatrix;
            Vector4 projInfo = new Vector4
                    ((-2.0f / (sWidth * P[0])),
                    (-2.0f / (sHeight * P[5])),
                    ((1.0f - P[2]) / P[0]),
                    ((1.0f + P[6]) / P[5]));

            Vector3 cameraClipInfo = (float.IsPositiveInfinity(camera_.farClipPlane)) ?
                new Vector3(camera_.nearClipPlane, -1.0f, 1.0f) :
                new Vector3(camera_.nearClipPlane * camera_.farClipPlane, camera_.nearClipPlane - camera_.farClipPlane, camera_.farClipPlane);

            material.SetVector(kReflectionBufferSize, new Vector2(rtW, rtH));
            material.SetVector(kScreenSize, new Vector2(sWidth, sHeight));
            material.SetVector(kInvScreenSize, new Vector2((float)(1.0f / sWidth), (float)(1.0f / sHeight)));
            material.SetVector(kProjInfo, projInfo); // used for unprojection

            material.SetVector(kCameraClipInfo, cameraClipInfo);

            Matrix4x4 warpToScreenSpaceMatrix = new Matrix4x4();
            warpToScreenSpaceMatrix.SetRow(0, new Vector4(sx, 0.0f, 0.0f, sx));
            warpToScreenSpaceMatrix.SetRow(1, new Vector4(0.0f, sy, 0.0f, sy));
            warpToScreenSpaceMatrix.SetRow(2, new Vector4(0.0f, 0.0f, 1.0f, 0.0f));
            warpToScreenSpaceMatrix.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

            Matrix4x4 projectToPixelMatrix = warpToScreenSpaceMatrix * P;

            material.SetMatrix(kProjectToPixelMatrix, projectToPixelMatrix);
            material.SetMatrix(kWorldToCameraMatrix, camera_.worldToCameraMatrix);
            material.SetMatrix(kCameraToWorldMatrix, camera_.worldToCameraMatrix.inverse);

            if (m_CommandBuffer == null)
            {
                m_CommandBuffer = new CommandBuffer();
                m_CommandBuffer.name = "Screen Space Reflections";

                // RGB: Normals, A: Roughness.
                // Has the nice benefit of allowing us to control the filtering mode as well.
                m_CommandBuffer.GetTemporaryRT(kNormalAndRoughnessTexture, -1, -1, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

                m_CommandBuffer.GetTemporaryRT(kHitPointTexture, rtW, rtH, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);

                for (int i = 0; i < maxMip; ++i)
                {
                    // We explicitly interpolate during bilateral upsampling.
                    m_CommandBuffer.GetTemporaryRT(kReflectionTextures[i], rtW >> i, rtH >> i, 0, FilterMode.Bilinear, intermediateFormat);
                }

                m_CommandBuffer.GetTemporaryRT(kFilteredReflections, rtW, rtH, 0, bilateralUpsample ? FilterMode.Point : FilterMode.Bilinear, intermediateFormat);
                m_CommandBuffer.GetTemporaryRT(kFinalReflectionTexture, rtW, rtH, 0, FilterMode.Point, intermediateFormat);

                m_CommandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, kNormalAndRoughnessTexture, material, (int)PassIndex.BilateralKeyPack);
                m_CommandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, kHitPointTexture, material, (int)PassIndex.RayTraceStep);
                m_CommandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, kFilteredReflections, material, (int)PassIndex.HitPointToReflections);
                m_CommandBuffer.Blit(kFilteredReflections, kReflectionTextures[0], material, (int)PassIndex.PoissonBlur);

                for (int i = 1; i < maxMip; ++i)
                {
                    int inputTex = kReflectionTextures[i - 1];

                    int lowMip = i;

                    m_CommandBuffer.GetTemporaryRT(kBlurTexture, rtW >> lowMip, rtH >> lowMip, 0, FilterMode.Bilinear, intermediateFormat);
                    m_CommandBuffer.SetGlobalVector(kAxis, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
                    m_CommandBuffer.SetGlobalFloat(kCurrentMipLevel, i - 1.0f);

                    m_CommandBuffer.Blit(inputTex, kBlurTexture, material, (int)PassIndex.Blur);

                    m_CommandBuffer.SetGlobalVector(kAxis, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));

                    inputTex = kReflectionTextures[i];
                    m_CommandBuffer.Blit(kBlurTexture, inputTex, material, (int)PassIndex.Blur);
                    m_CommandBuffer.ReleaseTemporaryRT(kBlurTexture);
                }

                m_CommandBuffer.Blit(kReflectionTextures[0], kFinalReflectionTexture, material, (int)PassIndex.CompositeSSR);

                m_CommandBuffer.GetTemporaryRT(kTempTexture, camera_.pixelWidth, camera_.pixelHeight, 0, FilterMode.Bilinear, intermediateFormat);

                m_CommandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, kTempTexture, material, (int)PassIndex.CompositeFinal);
                m_CommandBuffer.Blit(kTempTexture, BuiltinRenderTextureType.CameraTarget);


                m_CommandBuffer.ReleaseTemporaryRT(kTempTexture);
                camera_.AddCommandBuffer(CameraEvent.AfterFinalPass, m_CommandBuffer);
            }
        }
    }
}
