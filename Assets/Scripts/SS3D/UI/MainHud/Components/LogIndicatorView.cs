using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using IngameDebugConsole;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Settings;
using UnityEngine;

namespace SS3D.UI.MainHud.Components
{
    /// <summary>
    /// Shows the in-game console's info/warning/error counter when Settings > Display > Show Log Indicator is on. Lives
    /// for the whole process. The console keeps logging while the counter is hidden, so turning it on reports what
    /// already happened, and the toggle key still opens the log window.
    /// </summary>
    public sealed class LogIndicatorView : View
    {
        private static bool s_bootstrapped;

        private PlayerSettingsSubSystem _settings;

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
            GameObject host = new(nameof(LogIndicatorView));
            DontDestroyOnLoad(host);
            host.AddComponent<LogIndicatorView>();
        }

        protected override void OnStart()
        {
            base.OnStart();
            AddHandle(UpdateEvent.AddListener(HandleUpdate));
        }

        // Checked per frame rather than on Changed: DebugLogManager.Start overwrites PopupEnabled with its serialized
        // value, and both it and PlayerSettingsSubSystem may come up after this view.
        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            DebugLogManager console = DebugLogManager.Instance;
            if (console == null || (_settings == null && !SubSystems.TryGet(out _settings)))
            {
                return;
            }

            bool show = _settings.Settings.Display.ShowLogIndicator;
            if (console.PopupEnabled != show)
            {
                console.PopupEnabled = show;
            }
        }
    }
}
