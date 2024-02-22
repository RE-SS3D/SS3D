using SS3D.Permissions;
using SS3D.Systems;
using UnityEngine;

namespace SS3D.Engine.Chat
{
    [CreateAssetMenu(fileName = "New Chat Channel", menuName = "SS3D/UI/Chat/Chat Channel")]
    public class ChatChannel : ScriptableObject
    {
        [Tooltip("Shown in front of the message sent, like [ABB]. Let empty for no abbreviation. Also used as tabName if tabName is empty.")]
        public string abbreviation;
        [Tooltip("Used on the tabs dropdown when selecting the tab to write to.")]
        public string tabName;
        public Color color;
        [Tooltip("If enabled, player will be able to exclude it from their InGame chat.")]
        public bool hidable;
        public ServerRoleTypes roleRequiredToUse;
        public bool canFormatText;
        public bool distanceBased;
        public float maxDistance;
        [Tooltip("Can be format, even if canFormatText is disabled. Useful for adding specific format to a channel")]
        public string prefix;
        [Tooltip("Can be format, even if canFormatText is disabled. Useful for adding specific format to a channel")]
        public string suffix;
        public bool hideSenderName;
        public Trait requiredTraitInHeadset;
    }
}