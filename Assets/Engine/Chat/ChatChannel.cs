using System;
using UnityEngine;

namespace SS3D.Engine.Chat
{
    [Serializable]
    public struct ChatChannel
    {
        public string Name;
        public string Abbreviation;
        public Color Color;
        public bool Hidable;
    }
}