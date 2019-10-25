using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SceneBuilder : EditorWindow
{
    [MenuItem("Window/Eric's SceneBuilder")]
    private static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(SceneBuilder));
    }

    private enum EditorFunction
    {
        add_tiles,
        del_tiles,
        build_furniture,

    }

    private EditorFunction editorFunction;
    private bool buildMode = false;
    private Tile.TileTypes selectedTileType;
    private int upperTurfStat;
    private int lowerTurfStat;
    private int y_select = 0;
    private GameObject buildTarget = null;

    private void DisplayVisualHelp(Vector3 cell)
    {
        // Vertices of our square
        Vector3 cube_1 = cell + new Vector3(.5f, 0f, .5f);
        Vector3 cube_2 = cell + new Vector3(.5f, 0f, -.5f);
        Vector3 cube_3 = cell + new Vector3(-.5f, 0f, -.5f);
        Vector3 cube_4 = cell + new Vector3(-.5f, 0f, .5f);

        // Rendering
        Handles.color = Color.green;
        Vector3[] lines = {cube_1, cube_2, cube_2, cube_3, cube_3, cube_4, cube_4, cube_1};
        Handles.DrawLines(lines);

        //Handles.color = Color.red;
        //Handles.DrawLine(mouse_ray.origin, mouse_ray.direction);
    }

    private Vector3 GetCell(){
        // Get the mouse position at y = 0
        Ray mouse_ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        Vector3 mousePos = mouse_ray.origin - mouse_ray.direction * (mouse_ray.origin.y / mouse_ray.direction.y);

        // Get the corresponding cell on our virtual grid
        Vector3 cell = new Vector3(Mathf.RoundToInt(mousePos.x), y_select ,Mathf.RoundToInt(mousePos.z));
        //Debug.Log(mousePos);
        //Debug.Log(cell);
        return cell;
    }

    private void OnGUI() { 
        buildMode = EditorGUILayout.Toggle("Enable Builder:", buildMode);
        editorFunction = (EditorFunction)EditorGUILayout.EnumPopup("Editor Function:", editorFunction);
        selectedTileType = (Tile.TileTypes)EditorGUILayout.EnumPopup("TileType:", selectedTileType);
        upperTurfStat = EditorGUILayout.IntField("Upper Turf Status:", upperTurfStat);
        lowerTurfStat = EditorGUILayout.IntField("Lower Turf Status:", lowerTurfStat);
        buildTarget = EditorGUILayout.ObjectField("Build Target: ", buildTarget, typeof(GameObject), false) as GameObject;
    }



    private void OnSceneGUI(SceneView sceneView)
    {
        if (buildMode)
        {
            Vector3 selectedCell = GetCell();
            DisplayVisualHelp(selectedCell);
            HandleSceneViewInputs(selectedCell);
            sceneView.Repaint();
        }
    }

    private bool isMouseDown = false;

    private void HandleSceneViewInputs(Vector3 cell)
    {
        // Filter the left click so that we can't select objects in the scene
        if (Event.current.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(0); // Consume the event
        }
        if (Event.current.type == EventType.ScrollWheel)
        {
            Debug.Log("SOUP");
        }
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            isMouseDown = true;
        }
        if (Event.current.type == EventType.MouseUp){
            isMouseDown = false;
        }
        if (isMouseDown){
            UpdateTiles(cell);
        }

    }

    void UpdateTiles(Vector3 cell){
        Debug.Log(string.Format("tile_{0}_{1}",cell.x, cell.z));
        GameObject selectedTile = null;

        switch(editorFunction)
        {
            case(EditorFunction.add_tiles):
                selectedTile = GameObject.Find(string.Format("tile_{0}_{1}",cell.x, cell.z));
                if (selectedTile == null) {
                    //create new tile
                    GameObject new_obj = Instantiate(Resources.Load("empty_tile"), cell, Quaternion.identity, GameObject.Find("TileLoader").transform) as GameObject;
                    new_obj.GetComponent<Tile>().TileDescriptor = selectedTileType;
                    new_obj.GetComponent<Tile>().initTile();
                    new_obj.GetComponent<Turf>().lowerState = lowerTurfStat;
                    new_obj.GetComponent<Turf>().upperState = upperTurfStat;
                    new_obj.GetComponent<Turf>().InitTurf();
                    new_obj.name = string.Format("tile_{0}_{1}", cell.x, cell.z);
                    GameObject.Find("TileLoader").GetComponent<Tile_loader>().tile_list.Add(new_obj);
                }else{
                    //updateTile
                    selectedTile.GetComponent<Tile>().TileDescriptor = selectedTileType;
                    selectedTile.GetComponent<Tile>().initTile();
                    selectedTile.GetComponent<Turf>().lowerState = lowerTurfStat;
                    selectedTile.GetComponent<Turf>().upperState = upperTurfStat;
                    selectedTile.GetComponent<Turf>().InitTurf();
                }
                break;
            case(EditorFunction.del_tiles):
                selectedTile = GameObject.Find(string.Format("tile_{0}_{1}",cell.x, cell.z));
                if (selectedTile != null) {
                    #if UNITY_EDITOR
                    DestroyImmediate(selectedTile);
                    #else
                    Destroy(selectedTile);
                    #endif
                    GameObject.Find("TileLoader").GetComponent<Tile_loader>().CleanList();
                }
                break;
            case(EditorFunction.build_furniture):
                if (buildTarget != null ){
                    GameObject.Find(string.Format("tile_{0}_{1}",cell.x, cell.z)).GetComponent<TileContentManager>().BuildContent(buildTarget, 0);
                }
                break;
        }

    }

    void OnFocus()
    {
        SceneView.duringSceneGui -= this.OnSceneGUI; // Just in case
        SceneView.duringSceneGui += this.OnSceneGUI;
    }

    void OnDestroy()
    {
        SceneView.duringSceneGui -= this.OnSceneGUI;
    }


}


