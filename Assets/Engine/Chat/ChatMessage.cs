using System;
using UnityEngine;

[Serializable]
public struct ChatMessage
{
    public string Sender;
    public string Text;
    public ChatChannel Channel;
}