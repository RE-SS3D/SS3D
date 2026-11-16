#if UNITY_EDITOR
using UnityEditor;

namespace SS3D.Editor
{
    /// <summary>
    /// One-time-per-session notice telling returning developers that the editor toolbar tools
    /// (Scene Switcher, Launcher and Network Settings) moved from the old UnityToolbarExtender
    /// plugin to Unity's native main toolbar during the Unity 6 upgrade.
    /// <para>
    /// This is disposable migration scaffolding: it only means anything to people who remember the
    /// old toolbar, so it should be deleted once the upgrade has propagated through the team. The
    /// durable way to discover the tools is the SS3D menu items and the README, not this dialog.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Gating uses two stores so an accidental dismissal never loses the message:
    /// <list type="bullet">
    /// <item><see cref="EditorPrefs"/> (<see cref="OptOutKey"/>) is the permanent, per-user opt-out,
    /// set only when the developer deliberately picks "Don't show again".</item>
    /// <item><see cref="SessionState"/> (<see cref="ShownKey"/>) survives domain reloads but is
    /// cleared when the editor quits, so the notice shows once per editor launch rather than on
    /// every recompile. A plain static field cannot do this because it resets on every reload.</item>
    /// </list>
    /// </remarks>
    [InitializeOnLoad]
    internal static class ToolbarMigrationNotice
    {
        /// <summary>
        /// EditorPrefs key for the permanent, per-user opt-out. Versioned so the notice can be
        /// re-issued later (for a future toolbar change) by bumping the suffix.
        /// </summary>
        private const string OptOutKey = "SS3D.ToolbarNotice.OptOut.v1";

        /// <summary>
        /// SessionState key marking that the notice has already been shown this editor session.
        /// </summary>
        private const string ShownKey = "SS3D.ToolbarNotice.ShownThisSession.v1";

        /// <summary>
        /// Body copy for the dialog, explaining where the tools went and how to re-enable them.
        /// </summary>
        private const string Message =
            "As part of the Unity 6 upgrade, the Scene Switcher, Launcher and Network Settings tools "
            + "moved from the old toolbar plugin to Unity's native main toolbar.\n\n"
            + "If you don't see them, right-click the main toolbar and enable them under Tools.";

        /// <summary>
        /// Runs on every domain load. Bails out for batch builds, for developers who opted out, and
        /// when the notice has already been shown this session; otherwise defers the dialog to the
        /// next editor update so it pops after the editor UI is ready rather than mid-initialisation.
        /// </summary>
        static ToolbarMigrationNotice()
        {
            if (UnityEngine.Application.isBatchMode)
            {
                return;
            }

            if (EditorPrefs.GetBool(OptOutKey, false))
            {
                return;
            }

            if (SessionState.GetBool(ShownKey, false))
            {
                return;
            }

            EditorApplication.delayCall += Show;
        }

        /// <summary>
        /// Shows the migration notice once. The keep-alive option is the cancel button so the
        /// reflexive dismissals (Escape and the window close box, which both return the cancel value)
        /// keep the notice for next launch; only the deliberate "Don't show again" affirmative button
        /// sets the permanent opt-out.
        /// </summary>
        private static void Show()
        {
            if (SessionState.GetBool(ShownKey, false))
            {
                return;
            }

            SessionState.SetBool(ShownKey, true);

            bool optOut = EditorUtility.DisplayDialog(
                "Where did the SS3D toolbar tools go?",
                Message,
                "Don't show again",
                "Remind me later");

            if (optOut)
            {
                EditorPrefs.SetBool(OptOutKey, true);
            }
        }
    }
}
#endif