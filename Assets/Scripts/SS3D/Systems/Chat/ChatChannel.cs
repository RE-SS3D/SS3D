using UnityEngine;

namespace SS3D.Engine.Chat
{
    [CreateAssetMenu(fileName = "New Chat Channel", menuName = "SS3D/UI/Chat/Chat Channel")]
    public class ChatChannel : ScriptableObject
    {
        public string abbreviation;
        public Color color;
        public bool hidable;
    }
}