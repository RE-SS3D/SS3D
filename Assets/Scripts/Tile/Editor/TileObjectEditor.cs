using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TileMap.Editor
{
    [CustomEditor(typeof(TileObject))]
    public class TileObjectEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            TileObject tile = (TileObject)target;

            
            if(extraState == null) {
                var states = tile.GetComponentsInChildren(typeof(TileStateCommunicator));
                extraState = states.Select(state => new Tuple<string, SerializedObject>(state.gameObject.name, new SerializedObject(state))).ToDictionary(x => x.Item1, x => x.Item2);
            }

            // Get to the drawing
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tile"), true);

            foreach (var state in extraState) {
                state.Value.Update();
                EditorGUILayout.PropertyField(state.Value.FindProperty("tileState"), new GUIContent(state.Key + " State"), true);
                state.Value.ApplyModifiedProperties();
            }
            serializedObject.ApplyModifiedProperties();
        }

        private Dictionary<string, SerializedObject> extraState;
    }
}