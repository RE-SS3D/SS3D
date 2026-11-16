using SS3D.Systems.Entities;
using UnityEngine;

namespace SS3D.Systems.Comms
{
    /// <summary>Text and fade rules for local speech and action chips (comms.md §3, action-messages.md §5).</summary>
    internal static class LocalSpeechChipText
    {
        private const float FadeOutTailSeconds = 1f;

        internal static string ResolveNameLabel(SpeechMode mode, string speakerName, bool isNewest)
        {
            // Local-chat head chips only. Radio / announcements use other UI surfaces and never
            // supply a name label through this path.
            if (!isNewest || mode is SpeechMode.Whisper or SpeechMode.Emote
                or SpeechMode.Radio or SpeechMode.Announcement or SpeechMode.Action)
            {
                return string.Empty;
            }

            return speakerName;
        }

        internal static string FormatDisplayText(string text, SpeechMode mode, int garbleSeed, AudibilityTier tier, string speakerName, bool _)
        {
            string body = tier == AudibilityTier.Clear
                ? text
                : TextGarbler.Garble(text, garbleSeed);

            return mode switch
            {
                // Mock 1a: name folded into the line, italic via CSS, no asterisks.
                SpeechMode.Emote => $"{speakerName} {body}",
                SpeechMode.Radio => body,
                SpeechMode.Announcement => body,
                SpeechMode.Whisper => body,
                SpeechMode.Action => body,
                SpeechMode.Shout => body.ToUpperInvariant(),
                _ => body,
            };
        }

        internal static float StackOpacity(int fromNewest)
        {
            return fromNewest switch
            {
                0 => 1f,
                1 => 0.65f,
                _ => 0.4f,
            };
        }

        internal static float ComputeFadeOpacity(float expiryTime)
        {
            float remaining = expiryTime - Time.time;
            if (remaining >= FadeOutTailSeconds)
            {
                return 1f;
            }

            return Mathf.Clamp01(remaining / FadeOutTailSeconds);
        }

        internal static string ResolveSpeakerName(Entity speaker)
        {
            Mind mind = speaker.Mind;
            if (mind != null && mind != Mind.Empty && !string.IsNullOrEmpty(mind.DisplayName))
            {
                return mind.DisplayName;
            }

            return "Someone";
        }
    }
}
