using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using static Tile.TileMapEditorHelpers;

namespace Engine.Tiles.Editor.TileMap
{
    public class TileMapEditor : EditorWindow
    {
        [MenuItem("Window/TileMap Editor")]
        public static void ShowWindow()
        {
            GetWindow(typeof(TileMapEditor)).Show();
        }

        public void OnEnable()
        {
            // Get or create the tilemanager
            tileManager = FindObjectOfType<TileManager>();
            DestroyAllGhosts(tileManager);

            tiles.Update(tileManager);

            SceneView.duringSceneGui += OnSceneGUI;
        }
        public void OnDisable()
        {
            if(dragHandler != null) {
                dragHandler.CancelDrag();
            }

            tiles.Destroy();
            DestroyAllGhosts(tileManager);

            SceneView.duringSceneGui -= OnSceneGUI;
        }

        public void OnGUI()
        {
            if(tileManager == null)
                tileManager = FindObjectOfType<TileManager>();

            tiles.Update(tileManager);

            EditorGUILayout.Space();

            for (int i = 0; i < tiles.Definitions.Count; ++i) {
                tiles.ShowInspectorFor(i);

                // Now add some actions for the tile
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space();
                if (GUILayout.Button("Add Tiles")) {
                    if(tiles.Objects[i] != null)
                        SelectTile(i);
                    else
                        Debug.LogWarning("Can't add tiles - tile definition is empty");
                }
                if (GUILayout.Button("Remove Tile Definition")) {
                    tiles.RemoveAt(i);
                }
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Add Tile Type")) {
                tiles.Add();
            }

            if (GUILayout.Button("Refresh TileMap")) {
                tileManager.ReinitializeFromChildren();
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if(selectedTileIndex == -1)
                return;
            
            var selectionTile = tiles.Objects[selectedTileIndex];
            // Ensure the user can't use other scene controls whilst this one is active.
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            // Convert mouse position to world position by finding point where y = 0.
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            Vector3 position = ray.origin - (ray.origin.y / ray.direction.y) * ray.direction;
            Vector3 snappedPosition = tileManager.GetPositionClosestTo(position);

            // Set ghost tile's position
            selectionTile.transform.position = snappedPosition;
            Vector2Int tilePosition = tileManager.GetIndexAt(snappedPosition);

            // Dragging handle - hold shift and drag mouse to paint area
            if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) && Event.current.shift && Event.current.button == 0) {
                if(dragHandler == null) {
                    // Hide the normal tile selector
                    tiles.HideTile(selectedTileIndex);

                    dragHandler = new TileDragHandler(tileManager, tiles.Definitions[selectedTileIndex], tilePosition);
                }
                dragHandler.HandleDrag(tilePosition);
            }
            else if((Event.current.type == EventType.MouseUp && Event.current.button == 0) && dragHandler != null) {
                dragHandler.EndDrag();
                dragHandler = null;
                
                // Reshow the normal tile selector
                tiles.ShowTile(selectedTileIndex);
            }
            // (Simpler) placing handle - click to place
            else if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) && Event.current.button == 0) {
                if(lastPlacement != tilePosition) {
                    lastPlacement = tilePosition;
                    SetTile(tileManager, tiles.Definitions[selectedTileIndex], tilePosition.x, tilePosition.y);
                }
            }
            // If the user presses escape, stop the control
            else if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape) {
                if(dragHandler != null) {
                    dragHandler.CancelDrag();
                    dragHandler = null;
                }

                SelectTile(-1);
            }
        }
        
        /**
         * Switches currently in-use ghost tile to the one specified
         */
        private void SelectTile(int index)
        {
            if(selectedTileIndex != -1)
                tiles.HideTile(selectedTileIndex);

            selectedTileIndex = index;
            if(selectedTileIndex != -1)
                tiles.ShowTile(index);
        }
        
        private TileManager tileManager;

        private TileSet tiles = new TileSet();

        // Stuff for editing scene
        private int selectedTileIndex = -1;
        private Vector2Int lastPlacement;

        private TileDragHandler dragHandler;
    }
}