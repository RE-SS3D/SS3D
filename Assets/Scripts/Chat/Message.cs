using System;
using UnityEngine;

[Serializable]
public struct Message
{
    public string Sender;
    public string Text;
    public ChatChannel Channel;
}