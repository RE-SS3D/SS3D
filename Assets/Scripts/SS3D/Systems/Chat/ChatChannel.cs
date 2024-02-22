using SS3D.Permissions;
using SS3D.Systems;
using UnityEngine;

namespace SS3D.Engine.Chat
{
    [CreateAssetMenu(fileName = "New Chat Channel", menuName = "SS3D/UI/Chat/Chat Channel")]
    public class ChatChannel : ScriptableObject
    {
        [Tooltip("Shown in front of the message sent, like [ABB]. Let empty for no abbreviation. Also used as tabName if tabName is empty.")]
        public string Abbreviation;
        [Tooltip("Used on the tabs dropdown when selecting the tab to write to.")]
        public string TabName;
        public Color Color;
        [Tooltip("If enabled, player will be able to exclude it from their InGame chat.")]
        public bool Hidable;
        public ServerRoleTypes RoleRequiredToUse;
        public bool CanFormatText;
        public bool DistanceBased;
        public float MaxDistance;
        [Tooltip("Can be format, even if canFormatText is disabled. Useful for adding specific format to a channel")]
        public string Prefix;
        [Tooltip("Can be format, even if canFormatText is disabled. Useful for adding specific format to a channel")]
        public string Suffix;
        public bool HideSenderName;
        public Trait RequiredTraitInHeadset;
    }
}