using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MainHud.Components
{
    /// <summary>
    /// Small frame-rate readout in the top-left corner, shown when Settings > Display > Show FPS is on. Lives for the
    /// whole process (lobby, round, map editor). It averages over each refresh interval, so it costs nothing per frame.
    /// </summary>
    public sealed class FpsCounterView : View
    {
        private const long RefreshMilliseconds = 500;

        /// <summary>Above the HUD, lobby and dev windows; the label never takes input.</summary>
        private const float SortingOrder = 3000f;

        private static bool s_bootstrapped;

        private UIDocument _document;
        private Label _label;
        private PlayerSettingsSubSystem _settings;
        private int _lastFrame;
        private float _lastTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_bootstrapped = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (s_bootstrapped || PlayerSettingsSubSystem.IsHeadless)
            {
                return;
            }

            s_bootstrapped = true;
            GameObject host = new(nameof(FpsCounterView));
            DontDestroyOnLoad(host);
            host.AddComponent<FpsCounterView>();
        }

        protected override void OnStart()
        {
            base.OnStart();

            _document = gameObject.AddComponent<UIDocument>();
            _document.panelSettings = Resources.Load<MainHudAssetCatalog>(MainHudAssetPaths.ResourcesCatalogName)?.PanelSettings;
            _document.sortingOrder = SortingOrder;

            VisualElement root = _document.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;

            _label = new Label { name = "fps-counter", pickingMode = PickingMode.Ignore };
            _label.style.position = UnityEngine.UIElements.Position.Absolute;
            _label.style.left = 4f;
            _label.style.top = 2f;
            _label.style.fontSize = 11;
            _label.style.color = new Color(1f, 1f, 1f, 0.55f);
            _label.style.display = DisplayStyle.None;
            root.Add(_label);

            _lastFrame = Time.frameCount;
            _lastTime = Time.realtimeSinceStartup;
            root.schedule.Execute(Refresh).Every(RefreshMilliseconds);
        }

        private void Refresh()
        {
            // PlayerSettingsSubSystem may come up after this view; keep looking until it exists.
            if (_settings == null)
            {
                SubSystems.TryGet(out _settings);
            }

            bool show = _settings != null && _settings.Settings.Display.ShowFps;
            _label.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

            int frame = Time.frameCount;
            float now = Time.realtimeSinceStartup;
            float elapsed = now - _lastTime;
            int frames = frame - _lastFrame;
            _lastFrame = frame;
            _lastTime = now;

            if (show && elapsed > 0f && frames > 0)
            {
                float fps = frames / elapsed;
                _label.text = $"{Mathf.RoundToInt(fps)} FPS  {1000f / fps:0.0} ms";
            }
        }
    }
}
