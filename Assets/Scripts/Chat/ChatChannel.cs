using System;
using UnityEngine;

[Serializable, CreateAssetMenu(fileName = "New Chat Channel", menuName = "Chat/Channel")]
public class ChatChannel : ScriptableObject
{
    public string Abbreviation;
    public Color Color;
}