using System;
using UnityEngine;

[Serializable]
public struct Message
{
    public GameObject Sender;
    public string Text;
    public ChatChannel Channel;
}