using UnityEngine;
using UnityEditor;
using TileMap;
using System;
using System.Collections.Generic;

public class TileMapEditor : EditorWindow
{
    // We have to put it in a special container so that it's serializable
    class ExtraArgsContainer : ScriptableObject
    {
        public ConstructibleTile defaultTile;
    }

    [MenuItem("Window/TileMap Editor")]
    public static void ShowWindow()
    {
        GetWindow(typeof(TileMapEditor));
    }

    public void OnEnable()
    {
        // TODO: These args need to be saved so they reappear
        extraArgs = CreateInstance<ExtraArgsContainer>();
        extraArgsSerialized = new SerializedObject(extraArgs);
        defaultTileProperty = extraArgsSerialized.FindProperty("defaultTile");

        // Get or create the tilemanager
        tileManager = FindObjectOfType<TileManager>();

        SceneView.duringSceneGui += this.OnSceneGUI;
    }
    public void OnDisable()
    {
        SceneView.duringSceneGui -= this.OnSceneGUI;
    }

    public void OnGUI()
    {
        if(tileManager == null)
            tileManager = FindObjectOfType<TileManager>();

        extraArgsSerialized.Update();
        EditorGUILayout.PropertyField(defaultTileProperty, true);
        extraArgsSerialized.ApplyModifiedProperties();

        if (GUILayout.Button("Add Tile")) {
            selectedTile = CreateGhostTile();
        }

        if (GUILayout.Button("Refresh TileMap")) {
            tileManager.ReinitializeFromChildren();
        }
    }


    private void OnSceneGUI(SceneView sceneView)
    {
        if(selectedTile != null) {
            // Ensure the user can't use other scene controls whilst this one is active.
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            // Convert mouse position to world position by finding point where y = 0.
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            Vector3 position = ray.origin - (ray.origin.y / ray.direction.y) * ray.direction;

            Vector3 snappedPosition = new Vector3(
                Mathf.Round(position.x - tileManager.Origin.x) + tileManager.Origin.x,
                0,
                Mathf.Round(position.z - tileManager.Origin.y) + tileManager.Origin.y
            );
            // Set ghost tile's position
            selectedTile.transform.position = snappedPosition;
            Vector2Int tilePosition = tileManager.GetIndexAt(snappedPosition);

            // Dragging handle - hold shift and drag mouse to paint area
            if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) && Event.current.shift && Event.current.button == 0) {
                HandleDrag(tilePosition);
            }
            else if((Event.current.type == EventType.MouseUp && Event.current.button == 0) && dragStartPosition.HasValue) {
                EndDrag();
            }
            // (Simpler) placing handle - click to place
            else if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) && Event.current.button == 0) {
                if(lastPlacement != tilePosition) {
                    lastPlacement = tilePosition;

                    if (tileManager.GetTile(snappedPosition) != null)
                        tileManager.UpdateTile(snappedPosition, extraArgs.defaultTile);
                    else
                        tileManager.CreateTile(snappedPosition, extraArgs.defaultTile);
                }
            }
            // If the user presses escape, or right click, stop the control
            else if ((Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape) ||
                (Event.current.type == EventType.MouseDown && Event.current.button == 1)) {
                // Ensure that when destroyed, this tile isn't mistaken for another one
                selectedTile.transform.position = new Vector3(-1000.0f, 0.0f, -1000.0f);
                DestroyImmediate(selectedTile.gameObject);
                selectedTile = null;
            }
        }
    }
    
    /**
     * Warning: Here be dragons.
     * 
     * This code handles creating the square showing a dragging rectangle of tiles on the screen
     * It has to deal with any number of rows or columns changing, even to the point where the rectangle may flip axes.
     * 
     * TODO: Deal with if the index is out-of-bounds.
     */
    private void HandleDrag(Vector2Int mousePosition)
    {
        if(dragTiles.Count == 0)
            dragStartPosition = null;

        if (!dragStartPosition.HasValue) {
            dragStartPosition = mousePosition;
            dragEndPosition = mousePosition;
            
            var tile = CreateGhostTile(" [" + mousePosition.x.ToString() + ", " + mousePosition.y.ToString() + "]");
            tile.transform.position = tileManager.GetPosition(mousePosition.x, mousePosition.y);
            dragTiles.Insert(0, tile);
        }
        else if (dragEndPosition != mousePosition) {

            // Change the number of columns and rows of tiles to match
            // Note: This is kinda insane.
            int rowStart = dragStartPosition.Value.x;
            int rowEnd = dragEndPosition.x;
            int rowInc = rowStart <= rowEnd ? 1 : -1;
            int columnStart = dragStartPosition.Value.y;
            int columnEnd = dragEndPosition.y;
            int columnInc = columnStart <= columnEnd ? 1 : -1;

            int rowLength = 1 + Math.Abs(rowEnd - rowStart);
            int columnLength = 1 + Math.Abs(columnEnd - columnStart);

            // Update columns
            int columnDiff = mousePosition.y - dragEndPosition.y;

            if (columnDiff * columnInc < 0) {
                for (int i = rowLength - 1; i >= 0; i--) {
                    // Remove tiles from (and including) columnEnd down to (and exluding) columnEnd - columnDiff
                    int minColumn = Math.Max(columnLength - Math.Abs(columnDiff), 1);
                    for (int j = minColumn; j < columnLength; j++) {
                        DestroyImmediate(dragTiles[columnLength * i + j].gameObject);
                    }
                    if (columnLength - minColumn > 0)
                        dragTiles.RemoveRange(columnLength * i + minColumn, columnLength - minColumn);
                }

                // We need to add more columns in the negative direction,
                // so we trigger the next if with updated values
                if(columnLength - Math.Abs(columnDiff) <= 0) {
                    // We've already deleted all but one column, so:
                    columnEnd = columnStart;
                    columnLength = 1;
                    columnDiff = mousePosition.y - dragStartPosition.Value.y;
                    // Flip the incrementor, causing columnDiff * columnInc > 0
                    columnInc = -columnInc;
                }
            }
            if (columnDiff * columnInc > 0) {
                for (int i = rowLength - 1; i >= 0; i--) { // Go backwards so we can add and remove from array without messing with indices
                    // Add tiles from (but excluding) columnEnd, up to (and including) columnEnd + columnDiff
                    for (int j = columnLength; j < columnLength + Math.Abs(columnDiff); j++) {
                        var tile = CreateGhostTile(" [" + (rowStart + i * rowInc).ToString() + ", " + (columnStart + j * columnInc).ToString() + "]");
                        tile.transform.position = tileManager.GetPosition(rowStart + i * rowInc, columnStart + j * columnInc);

                        dragTiles.Insert(columnLength * i + j, tile);
                    }
                }
            }

            columnEnd = Convert.ToInt32(mousePosition.y);
            columnInc = columnStart <= columnEnd ? 1 : -1;
            columnLength = 1 + Math.Abs(columnEnd - columnStart);

            // Now update rows
            int rowDiff = mousePosition.x - dragEndPosition.x;
            if (rowDiff * rowInc < 0) {
                int minRow = Math.Max(rowLength - Math.Abs(rowDiff), 1);

                for (int i = rowLength - 1; i >= minRow; i--) {
                    for (int j = 0; j < columnLength; j++) {
                        DestroyImmediate(dragTiles[columnLength * i + j].gameObject);
                    }
                }
                if(rowLength - minRow > 0)
                    dragTiles.RemoveRange(minRow * columnLength, (rowLength - minRow) * columnLength);

                // If we've crossed axes, we need to create more rows, so we manipulate values for the next if to trigger
                if (rowLength - Math.Abs(rowDiff) <= 0) {
                    // We've already deleted all but one row, so:
                    rowEnd = rowStart;
                    rowLength = 1;
                    rowDiff = mousePosition.x - dragStartPosition.Value.x;

                    // Flip the incrementor, so that the code now things we're increasing rows
                    rowInc = -rowInc;
                }
            }
            if (rowDiff * rowInc > 0) {
                for (int i = rowLength; i < rowLength + Math.Abs(rowDiff); i++) {
                    for (int j = 0; j < columnLength; j++) {
                        var tile = CreateGhostTile(" [" + (rowStart + i * rowInc).ToString() + ", " + (columnStart + j * columnInc).ToString() + "]");
                        tile.transform.position = tileManager.GetPosition(rowStart + i * rowInc, columnStart + j * columnInc);

                        dragTiles.Insert(columnLength * i + j, tile);
                    }
                }
            }

            dragEndPosition = mousePosition;
        }
    }
    private void EndDrag()
    {
        try {
            for(int i = 0; i < dragTiles.Count; ++i) {
                DestroyImmediate(dragTiles[i].gameObject);
            }
        }
        catch(Exception) { }
        dragTiles.Clear();
       
        int xInc = dragStartPosition.Value.x < dragEndPosition.x ? 1 : -1;
        int yInc = dragStartPosition.Value.y < dragEndPosition.y ? 1 : -1;
        for(int x = dragStartPosition.Value.x; x != dragEndPosition.x + xInc; x += xInc) {
            for(int y = dragStartPosition.Value.y; y != dragEndPosition.y + yInc; y += yInc) {

                if(tileManager.GetTile(x, y) == null)
                    tileManager.CreateTile(x, y, extraArgs.defaultTile);
                else
                    tileManager.UpdateTile(x, y, extraArgs.defaultTile);
            }
        }
        dragStartPosition = null;
    }

    private TileObject CreateGhostTile(string append = "")
    {
        // Create the tile
        var gameObject = new GameObject("Ghost Tile" + append);
        var tile = gameObject.AddComponent<TileObject>();
        tile.Tile = extraArgs.defaultTile;

        var meshes = gameObject.GetComponentsInChildren<MeshRenderer>();
        foreach(var mesh in meshes) {
            mesh.sharedMaterial.color = mesh.sharedMaterial.color * new Color(1.0f, 1.0f, 1.0f, 0.5f);
        }

        return tile;
    }

    private TileManager tileManager;

    // Args
    private ExtraArgsContainer extraArgs;
    // Used for rendering controls about extraArgs
    private SerializedObject extraArgsSerialized;
    private SerializedProperty defaultTileProperty;

    // Stuff for editing scene
    private TileObject selectedTile;
    private Vector2Int lastPlacement;

    private Vector2Int? dragStartPosition;
    private Vector2Int dragEndPosition;
    private List<TileObject> dragTiles = new List<TileObject>();
}