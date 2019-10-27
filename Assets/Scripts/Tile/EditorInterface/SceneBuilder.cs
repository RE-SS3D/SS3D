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
        build_furniture,
        build_disposal,
        build_blue,
        build_red
    }

    private EditorFunction editorFunction;
    private bool buildMode = false;
    private bool deleteMode = false;
    private Tile.TileTypes selectedTileType;
    private int upperTurfStat;
    private int lowerTurfStat;
    private int y_select = 0;

    private string[] furnitureTypes;
    private int furnitureTypes_index = 0;
    private List<GameObject> buildTargets = new List<GameObject>();
    private List<GUIContent> buildTargets_icons = new List<GUIContent>();
    private int buildTargets_index;
    private GameObject buildTarget = null;

    private bool pipeConnectN = false;
    private bool pipeConnectE = false;
    private bool pipeConnectS = false;
    private bool pipeConnectW = false;
    private bool pipeConnectAuto = false;


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
        buildMode = EditorGUILayout.Toggle("Enable Editor:", buildMode);
        editorFunction = (EditorFunction)EditorGUILayout.EnumPopup("Editor Function:", editorFunction);
        deleteMode = EditorGUILayout.Toggle("Delete?:", deleteMode);
        y_select = EditorGUILayout.IntField("Y-Level: ", y_select);
        DrawUILine(Color.gray);
        switch(editorFunction)
        {
            case(EditorFunction.add_tiles):
                selectedTileType = (Tile.TileTypes)EditorGUILayout.EnumPopup("TileType:", selectedTileType);
                upperTurfStat = EditorGUILayout.IntField("Upper Turf Status:", upperTurfStat);
                lowerTurfStat = EditorGUILayout.IntField("Lower Turf Status:", lowerTurfStat);
                break;
            case(EditorFunction.build_furniture):
                LoadFurnitureTypes();
                LoadPrefabFurniture(furnitureTypes[furnitureTypes_index]);
                //buildTarget = EditorGUILayout.ObjectField("Build Target: ", buildTarget, typeof(GameObject), false) as GameObject;
                break;
            case(EditorFunction.build_disposal):
                //buildTarget = EditorGUILayout.ObjectField("Build Target: ", buildTarget, typeof(GameObject), false) as GameObject;
                pipeConnectN = EditorGUILayout.Toggle("Connect N:", pipeConnectN);
                pipeConnectE = EditorGUILayout.Toggle("Connect E:", pipeConnectE);
                pipeConnectS = EditorGUILayout.Toggle("Connect S:", pipeConnectS);
                pipeConnectW = EditorGUILayout.Toggle("Connect W:", pipeConnectW);
                pipeConnectAuto = EditorGUILayout.Toggle("Auto Connect:", pipeConnectAuto);
                break;
            case(EditorFunction.build_blue):
                //buildTarget = EditorGUILayout.ObjectField("Build Target: ", buildTarget, typeof(GameObject), false) as GameObject;
                pipeConnectN = EditorGUILayout.Toggle("Connect N:", pipeConnectN);
                pipeConnectE = EditorGUILayout.Toggle("Connect E:", pipeConnectE);
                pipeConnectS = EditorGUILayout.Toggle("Connect S:", pipeConnectS);
                pipeConnectW = EditorGUILayout.Toggle("Connect W:", pipeConnectW);
                pipeConnectAuto = EditorGUILayout.Toggle("Auto Connect:", pipeConnectAuto);
                break;
            case(EditorFunction.build_red):
                //buildTarget = EditorGUILayout.ObjectField("Build Target: ", buildTarget, typeof(GameObject), false) as GameObject;
                pipeConnectN = EditorGUILayout.Toggle("Connect N:", pipeConnectN);
                pipeConnectE = EditorGUILayout.Toggle("Connect E:", pipeConnectE);
                pipeConnectS = EditorGUILayout.Toggle("Connect S:", pipeConnectS);
                pipeConnectW = EditorGUILayout.Toggle("Connect W:", pipeConnectW);
                pipeConnectAuto = EditorGUILayout.Toggle("Auto Connect:", pipeConnectAuto);
                break;
        }
    }

    private void LoadFurnitureTypes(){
        string[] directories = System.IO.Directory.GetDirectories("Assets/Resources/Furniture/");
        List<string> furnitureTypes_list = new List<string>();
        foreach(var directory in directories){
            furnitureTypes_list.Add(new System.IO.DirectoryInfo(directory).Name);
        }
        furnitureTypes = furnitureTypes_list.ToArray();
        //furnitureTypes = directories;
        furnitureTypes_index = EditorGUILayout.Popup(furnitureTypes_index, furnitureTypes);
    }

    private void LoadPrefabFurniture(string folderName){
        buildTargets.Clear();
        buildTargets_icons.Clear();
        Object[] preFabs = Resources.LoadAll("Furniture/"+folderName);
        foreach (Object preFab in preFabs){
            buildTargets.Add(preFab as GameObject);
            Texture2D texture = AssetPreview.GetAssetPreview(preFab);
            buildTargets_icons.Add(new GUIContent(texture));
        }
        buildTargets_index = GUILayout.SelectionGrid(buildTargets_index, buildTargets_icons.ToArray(), 3);
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

    public static void DrawUILine(Color color, int thickness = 2, int padding = 10)
    {
        Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding+thickness));
        r.height = thickness;
        r.y+=padding/2;
        r.x-=2;
        r.width +=6;
        EditorGUI.DrawRect(r, color);
    }

    private bool isMouseDown = false;

    private void HandleSceneViewInputs(Vector3 cell)
    {
        // Filter the left click so that we can't select objects in the scene
        if (Event.current.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(0); // Consume the event
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
        //Debug.Log(string.Format("tile_{0}_{1}",cell.x, cell.z));
        GameObject selectedTile = null;
        switch(editorFunction)
        {
            case(EditorFunction.add_tiles):
                selectedTile = GameObject.Find(string.Format("tile_{0}_{1}",cell.x, cell.z));
                if(!deleteMode) {
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
                }else{
                    if (selectedTile != null) {
                        #if UNITY_EDITOR
                        DestroyImmediate(selectedTile);
                        #else
                        Destroy(selectedTile);
                        #endif
                        GameObject.Find("TileLoader").GetComponent<Tile_loader>().CleanList();
                    }
                }
                break;
            case(EditorFunction.build_furniture):
                buildTarget = buildTargets[buildTargets_index];
                if (!deleteMode){
                    if (buildTarget != null && GameObject.Find(string.Format("tile_{0}_{1}",cell.x, cell.z)) != null){
                        GameObject.Find(string.Format("tile_{0}_{1}",cell.x, cell.z)).GetComponent<TileContentManager>().BuildContent(buildTarget, 0);
                    }
                }else{
                    if(GameObject.Find(string.Format("tile_{0}_{1}",cell.x, cell.z)) != null){
                        GameObject.Find(string.Format("tile_{0}_{1}",cell.x, cell.z)).GetComponent<TileContentManager>().DeleteContents();
                    }
                }
                break;
            case(EditorFunction.build_disposal):
                selectedTile = GameObject.Find(string.Format("tile_{0}_{1}",cell.x, cell.z));
                if (selectedTile != null) {
                   if(!deleteMode){
                       int config = 0;
                        if(pipeConnectN){
                            config ^= 8;
                        }
                        if(pipeConnectE){
                            config ^= 4;
                        }
                        if(pipeConnectS){
                            config ^= 2;
                        }
                        if(pipeConnectW){
                            config ^= 1;
                        }
                        if(pipeConnectAuto){
                            config = -1;
                        }
                       selectedTile.GetComponent<TilePipeManager>().BuildDisposal(config);
                   }else{
                       selectedTile.GetComponent<TilePipeManager>().DeleteDisposal();
                   }
                }
                break;
            case(EditorFunction.build_blue):
                selectedTile = GameObject.Find(string.Format("tile_{0}_{1}",cell.x, cell.z));
                if (selectedTile != null) {
                   if(!deleteMode){
                       int config = 0;
                        if(pipeConnectN){
                            config ^= 8;
                        }
                        if(pipeConnectE){
                            config ^= 4;
                        }
                        if(pipeConnectS){
                            config ^= 2;
                        }
                        if(pipeConnectW){
                            config ^= 1;
                        }
                        if(pipeConnectAuto){
                            config = -1;
                        }
                       selectedTile.GetComponent<TilePipeManager>().BuildBlue(config);
                   }else{
                       selectedTile.GetComponent<TilePipeManager>().DeleteBlue();
                   }
                }
                break;
            case(EditorFunction.build_red):
                selectedTile = GameObject.Find(string.Format("tile_{0}_{1}",cell.x, cell.z));
                if (selectedTile != null) {
                   if(!deleteMode){
                       int config = 0;
                        if(pipeConnectN){
                            config ^= 8;
                        }
                        if(pipeConnectE){
                            config ^= 4;
                        }
                        if(pipeConnectS){
                            config ^= 2;
                        }
                        if(pipeConnectW){
                            config ^= 1;
                        }
                        if(pipeConnectAuto){
                            config = -1;
                        }
                       selectedTile.GetComponent<TilePipeManager>().BuildRed(config);
                   }else{
                       selectedTile.GetComponent<TilePipeManager>().DeleteRed();
                   }
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


