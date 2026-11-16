using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace SS3D.Systems.Settings
{
    /// <summary>
    /// Writes <see cref="PlayerDisplaySettings"/> into the engine: window (<see cref="Screen"/>), quality tier
    /// (<see cref="QualitySettings"/>, one URP asset per tier), render scale/upscaler on the active URP asset, camera
    /// anti-aliasing and UI scale on registered screen-space panels. In the editor it restores every asset it touched
    /// when play stops, because runtime writes to assets persist there.
    /// </summary>
    internal sealed class PlayerDisplayApplier
    {
        private readonly HashSet<PanelSettings> _panels = new();
        private PlayerAntiAliasing _antiAliasing = PlayerAntiAliasing.Smaa;
        private float _uiScale = 1f;
#if UNITY_EDITOR
        private readonly Dictionary<UniversalRenderPipelineAsset, (float Scale, UpscalingFilterSelection Filter)> _assetOriginals = new();
        private readonly Dictionary<PanelSettings, float> _panelOriginals = new();
        private int _editorQualityLevel = -1;
#endif

        public static bool StpSupported => STP.IsSupported();

        public static Vector2Int Native => new(Display.main.systemWidth, Display.main.systemHeight);

        public static Vector2Int[] ResolutionChoices()
        {
            Vector2Int[] choices = PlayerResolutions.Choices(Screen.resolutions.Select(r => new Vector2Int(r.width, r.height)));
            return choices.Length > 0 ? choices : new[] { Native };
        }

        public static Vector2Int CurrentResolution(PlayerDisplaySettings display) =>
            PlayerResolutions.Resolve(display.ResolutionWidth, display.ResolutionHeight, ResolutionChoices(), Native);

        public void Enable() => RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;

        public void Disable()
        {
            RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;
#if UNITY_EDITOR
            foreach (KeyValuePair<UniversalRenderPipelineAsset, (float Scale, UpscalingFilterSelection Filter)> pair in _assetOriginals)
            {
                if (pair.Key == null) continue;
                pair.Key.renderScale = pair.Value.Scale;
                pair.Key.upscalingFilter = pair.Value.Filter;
            }
            foreach (KeyValuePair<PanelSettings, float> pair in _panelOriginals)
                if (pair.Key != null) pair.Key.scale = pair.Value;
            if (_editorQualityLevel >= 0) QualitySettings.SetQualityLevel(_editorQualityLevel, false);
#endif
        }

        public void ApplyAll(PlayerDisplaySettings display)
        {
            ApplyWindow(display);
            ApplyQuality(display);
            ApplyUiScale(display.UiScale);
        }

        public void ApplyWindow(PlayerDisplaySettings display)
        {
#if UNITY_EDITOR
            // The Game view owns its size in the editor; Screen.SetResolution would only resize a build window.
            _ = display;
#else
            // Borderless always covers the desktop at native size. A smaller borderless swapchain is stretched
            // to the screen, but on Linux/X11 the pointer still reports desktop pixels, so every UI (map editor,
            // diegetic screens) clicked with a growing offset. Lower-res rendering is render scale's job.
            Vector2Int size = display.WindowMode == PlayerWindowMode.Borderless ? Native : CurrentResolution(display);
            FullScreenMode mode = display.WindowMode switch
            {
                PlayerWindowMode.Fullscreen => FullScreenMode.ExclusiveFullScreen,
                PlayerWindowMode.Windowed => FullScreenMode.Windowed,
                _ => FullScreenMode.FullScreenWindow,
            };
            RefreshRate refresh = Screen.resolutions.Where(r => r.width == size.x && r.height == size.y)
                .Select(r => r.refreshRateRatio).OrderByDescending(r => r.value).FirstOrDefault();
            Screen.SetResolution(size.x, size.y, mode, refresh);
#endif
        }

        /// <summary>Tier, then frame pacing (each level carries its own vSyncCount), then the URP asset fields.</summary>
        public void ApplyQuality(PlayerDisplaySettings display)
        {
            int level = Mathf.Clamp((int)display.Quality, 0, QualitySettings.count - 1);
#if UNITY_EDITOR
            if (_editorQualityLevel < 0) _editorQualityLevel = QualitySettings.GetQualityLevel();
#endif
            if (QualitySettings.GetQualityLevel() != level) QualitySettings.SetQualityLevel(level, true);

            FramePacing pacing = PlayerDisplayPlan.Pace(display.VSync, display.FrameCap);
            QualitySettings.vSyncCount = pacing.VSyncCount;
            UnityEngine.Application.targetFrameRate = pacing.TargetFrameRate;

            RenderPlan plan = PlayerDisplayPlan.Resolve(display, StpSupported);
            _antiAliasing = plan.AntiAliasing;
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset asset) return;
#if UNITY_EDITOR
            if (!_assetOriginals.ContainsKey(asset)) _assetOriginals[asset] = (asset.renderScale, asset.upscalingFilter);
#endif
            asset.renderScale = plan.RenderScale;
            asset.upscalingFilter = plan.Upscaler switch
            {
                PlayerUpscaler.Stp => UpscalingFilterSelection.STP,
                PlayerUpscaler.Fsr1 => UpscalingFilterSelection.FSR,
                PlayerUpscaler.Bilinear => UpscalingFilterSelection.Linear,
                _ => UpscalingFilterSelection.Auto,
            };
        }

        public void RegisterPanel(PanelSettings panel)
        {
            if (panel == null || !_panels.Add(panel)) return;
#if UNITY_EDITOR
            _panelOriginals[panel] = panel.scale;
#endif
            panel.scale = _uiScale;
        }

        public void ApplyUiScale(float scale)
        {
            _uiScale = scale;
            foreach (PanelSettings panel in _panels)
                if (panel != null) panel.scale = scale;
        }

        /// <summary>Player-view cameras only: render-texture cameras (preview booth, icon generator) keep their own AA.</summary>
        private void HandleBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera.cameraType != CameraType.Game || camera.targetTexture != null) return;
            if (!camera.TryGetComponent(out UniversalAdditionalCameraData data)) return;
            AntialiasingMode mode = _antiAliasing switch
            {
                PlayerAntiAliasing.Fxaa => AntialiasingMode.FastApproximateAntialiasing,
                PlayerAntiAliasing.Smaa => AntialiasingMode.SubpixelMorphologicalAntiAliasing,
                PlayerAntiAliasing.Taa => AntialiasingMode.TemporalAntiAliasing,
                _ => AntialiasingMode.None,
            };
            if (data.antialiasing != mode) data.antialiasing = mode;
        }
    }
}
