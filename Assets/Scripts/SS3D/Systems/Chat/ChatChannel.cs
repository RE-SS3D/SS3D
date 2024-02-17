using System;
using UnityEngine;

namespace SS3D.Engine.Chat
{
    [Serializable]
    public struct ChatChannel
    {
        public string name;
        public string abbreviation;
        public Color color;
        public bool hidable;
    }
}