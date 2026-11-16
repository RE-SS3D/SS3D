using SS3D.Core.Debugging;
using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Inputs;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SS3D.Systems.Comms
{
    /// <summary>
    /// PLACEHOLDER for remaining compose polish (mode pip hints per comms.md §5). T opens the
    /// real draft chip via LocalSpeechBubbleController + InputSubSystem.OpenLocalSpeechCompose.
    /// Its F1 debug-menu tool cycles local-chat test lines (speak / whisper / shout / emote) for presentation
    /// checks — radio and station announcements live on other UI surfaces. SystemsBootstrap creates it
    /// in debug builds (same as ScreenEffectsDebugMenuView).
    /// </summary>
    public sealed class LocalSpeechDebugTrigger : Actor
    {
        private readonly struct LocalTestLine
        {
            public readonly SpeechMode Mode;
            public readonly string Text;

            public LocalTestLine(SpeechMode mode, string text)
            {
                Mode = mode;
                Text = text;
            }
        }

        private static readonly LocalTestLine[] TestLines =
        {
            new(SpeechMode.Speak, "Cargo's here, someone sign for it."),
            new(SpeechMode.Speak, "Can someone fix atmos?"),
            new(SpeechMode.Whisper, "not on comms, ok?"),
            new(SpeechMode.Shout, "Need oxygen!"),
            new(SpeechMode.Shout, "Help! Fire in engineering!"),
            new(SpeechMode.Emote, "waves toward the console."),
            new(SpeechMode.Speak, "Anyone seen the captain."),
        };

        private static bool s_bootstrapped;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_bootstrapped = false;

        /// <summary>Called by <c>SystemsBootstrap</c> at cold start, in debug builds only (the F1 menu reaches it).</summary>
        public static void Bootstrap()
        {
            if (s_bootstrapped)
            {
                return;
            }

            s_bootstrapped = true;

            GameObject host = new(nameof(LocalSpeechDebugTrigger));
            DontDestroyOnLoad(host);
            host.AddComponent<LocalSpeechDebugTrigger>();
        }

        private Entity _localEntity;
        private int _lineIndex;

        protected override void OnAwake()
        {
            base.OnAwake();

            AddHandle(LocalPlayerObjectChanged.AddListener(HandlePlayerObjectChanged));
            DebugTools.Get(DebugToolNames.LocalSpeechLine).Invoked += SpeakTestLine;
        }

        protected override void OnDestroyed()
        {
            DebugTools.Get(DebugToolNames.LocalSpeechLine).Invoked -= SpeakTestLine;
            base.OnDestroyed();
        }

        private void HandlePlayerObjectChanged(ref EventContext context, in LocalPlayerObjectChanged e)
        {
            _localEntity = e.PlayerHasObject ? e.PlayerObject.GetComponent<Entity>() : null;
        }

        private void SpeakTestLine()
        {
            if (_localEntity == null || !_localEntity.TryGetComponent(out LocalSpeechEmitter emitter))
            {
                return;
            }

            LocalTestLine line = TestLines[_lineIndex % TestLines.Length];
            _lineIndex++;

            emitter.CmdSpeak(line.Text, line.Mode);
        }
    }
}
