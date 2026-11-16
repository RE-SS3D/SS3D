using SS3D.Permissions;
using UnityEngine;

namespace SS3D.Systems.Comms
{
    public enum CommsChannelKind : byte
    {
        /// <summary>Department / public radio — left screen-space feed.</summary>
        Radio = 0,
        /// <summary>Station announcements — top-middle banner; typically code/server only.</summary>
        Announcement = 1,
        /// <summary>OOC / system / meta — not Tab-writable this pass.</summary>
        Meta = 2,
    }

    /// <summary>
    /// Non-positional comms channel definition (radio, announcements, meta).
    /// Salvaged from legacy ChatChannel; local speak/whisper/shout use <see cref="SpeechMode"/> instead.
    /// </summary>
    [CreateAssetMenu(fileName = "New Comms Channel", menuName = "SS3D/Comms/Channel")]
    public class CommsChannel : ScriptableObject
    {
        [Tooltip("Short tag shown in feed headers, e.g. ENG. Falls back to a truncated asset name when empty.")]
        public string Abbreviation;

        [Tooltip("Secondary label after the arrow in radio headers, e.g. OPEN.")]
        public string DisplaySuffix = "OPEN";

        [Tooltip("Banner / feed title override. Announcements default to ALL-STATION when empty.")]
        public string DisplayTitle;

        public Color Color = Color.white;

        public CommsChannelKind Kind = CommsChannelKind.Radio;

        [Tooltip("If true, players cannot send on this channel — server/code only.")]
        public bool CodeOnlyChannel;

        [Tooltip("If true, Tab-compose can select this radio channel. Keep the Tab list short for now.")]
        public bool AvailableInCompose;

        [Tooltip(
            "Slash token for T-compose (e.g. announce → /announce). Empty: Radio uses lowercased Abbreviation (ENG → /eng); Announcement requires an explicit value.")]
        public string ComposePrefix;

        [Tooltip("Admin role gate (enforced when non-None).")]
        public ServerRoleTypes RoleRequiredToUse = ServerRoleTypes.None;

        [Tooltip("Headset trait required to hear/speak (data only until MVP2 gating).")]
        public Trait RequiredTraitInHeadset;

        /// <summary>Stable registry key — Unity asset name.</summary>
        public string Id => name;

        public string ResolveAbbreviation()
        {
            if (!string.IsNullOrWhiteSpace(Abbreviation))
            {
                return Abbreviation.Trim().ToUpperInvariant();
            }

            string source = name ?? string.Empty;
            if (source.Length <= 3)
            {
                return source.ToUpperInvariant();
            }

            // Engineering → ENG, Medical → MED, Public → PUB
            return source.Length >= 3
                ? source.Substring(0, 3).ToUpperInvariant()
                : source.ToUpperInvariant();
        }

        public string ResolveRadioHeader()
        {
            string suffix = string.IsNullOrWhiteSpace(DisplaySuffix) ? "OPEN" : DisplaySuffix.Trim().ToUpperInvariant();
            return $"{ResolveAbbreviation()} > {suffix}";
        }

        public string ResolveAnnouncementTitle()
        {
            if (!string.IsNullOrWhiteSpace(DisplayTitle))
            {
                return DisplayTitle.Trim().ToUpperInvariant();
            }

            return "ALL-STATION";
        }

        /// <summary>
        /// Slash token without leading slash (lowercase). Null/empty = not prefix-writable.
        /// </summary>
        public string ResolveComposePrefix()
        {
            if (!string.IsNullOrWhiteSpace(ComposePrefix))
            {
                return ComposePrefix.Trim().ToLowerInvariant();
            }

            if (Kind == CommsChannelKind.Radio)
            {
                return ResolveAbbreviation().ToLowerInvariant();
            }

            return null;
        }

        /// <summary>
        /// Whether players may target this channel via a leading <c>/prefix</c> in T-compose.
        /// Announcement is prefix-only (not Tab); radio also needs <see cref="AvailableInCompose"/>.
        /// </summary>
        public bool IsComposePrefixWritable()
        {
            if (CodeOnlyChannel || string.IsNullOrEmpty(ResolveComposePrefix()))
            {
                return false;
            }

            return Kind switch
            {
                CommsChannelKind.Radio => AvailableInCompose,
                CommsChannelKind.Announcement => true,
                _ => false,
            };
        }
    }
}
