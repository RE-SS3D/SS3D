#if UNITY_EDITOR
using UnityEngine;
using UDiscord;
using UnityEditor;

[CustomEditor(typeof(DiscordManager))]

public class DiscordOpenEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if(GUILayout.Button("Open Discord Editor"))
        {
            DiscordEditor.Open((DiscordManager)target);
        }
    }
}
#endif