using SS3D.Permissions;
using SS3D.Systems;
using SS3D.Systems.Entities;
using System;
using UnityEngine;

namespace SS3D.Engine.Chat
{
    [CreateAssetMenu(fileName = "New Chat Channel", menuName = "SS3D/UI/Chat/Chat Channel")]
    public class ChatChannel : ScriptableObject
    {
        [Tooltip("Shown in front of the message sent between brackets, like [ABB]. Let empty for no abbreviation.")]
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
        [Tooltip("The verb (in present) used for describing how the entity is saying it. Like 'says', 'whispers', 'shouts'.")]
        public string defaultVerb;
        [Tooltip("Can be format, even if canFormatText is disabled. Useful for adding specific format to a channel.")]
        public string TextPrefix;
        [Tooltip("Can be format, even if canFormatText is disabled. Useful for adding specific format to a channel.")]
        public string TextSuffix;
        public bool HideSenderName;
        public Trait RequiredTraitInHeadset;

        public string GetColorTagOpening() => $"<color=#{ColorUtility.ToHtmlStringRGBA(Color)}>";

        public string GetColorTagClosing() => "</color>";
        
        public string GetTabText()
        {
            if (!string.IsNullOrEmpty(Abbreviation))
            {
                return $"[{Abbreviation}] ";
            }

            return "";
        }
        
        public string GetTextBeforeMessage(Player player)
        {
            if (HideSenderName)
            {
                return "";
            }

            if (player == null)
            {
                throw new NullReferenceException("No player found for sending a message in a channel where sender name is required");
            }
            
            if (!string.IsNullOrEmpty(Abbreviation))
            {
                return $"{player.Ckey}: ";
            }

            // This is a good place to add all functionality about specific verbs depending on the character situation
            // Also player.Ckey should probably be replaced with the character name
            
            return $"{player.Ckey} {defaultVerb}, \"";
        }
        
        public string GetTextAfterMessage()
        {
            if (HideSenderName || !string.IsNullOrEmpty(Abbreviation))
            {
                return "";
            }
            
            return "\"";
        }
    }
}