using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SS3D.Engine.Tiles.State;

namespace SS3D.Engine.Tiles.Editor
{
    [CustomEditor(typeof(TileObject))]
    public class TileObjectEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            TileObject tile = (TileObject)target;

            // Update children if state has changed.
            // Note: We can't just check whether the TileDefinition has changed, because this code gets called
            // FUCKING BEFORE the tile's components are updated, but after the tile has received the new TileDefinition.
            var subTiles = tile.GetComponentsInChildren(typeof(TileStateCommunicator));
            if (extraState == null || subTiles.Length != extraState.Count || subTiles.Any(subTile => !extraState.ContainsKey(subTile.gameObject.name))) {
                extraState = subTiles.Select(state => new Tuple<string, SerializedObject>(state.gameObject.name, new SerializedObject(state))).ToDictionary(x => x.Item1, x => x.Item2);
            }

            // Get to the drawing
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tile"), true);

            bool stateChanged = false;
            foreach (var state in extraState) {
                state.Value.Update();
                EditorGUILayout.PropertyField(state.Value.FindProperty("tileState"), new GUIContent(state.Key + " State"), true);
                stateChanged |= state.Value.ApplyModifiedProperties();
            }
            serializedObject.ApplyModifiedProperties();

            // If the tile's subobjects have been modified, apply those
            // changes to the tile definition
            if(stateChanged) {
                tile.RefreshSubData();
            }
        }

        private TileDefinition prevDefinition;
        private Dictionary<string, SerializedObject> extraState;
    }
}