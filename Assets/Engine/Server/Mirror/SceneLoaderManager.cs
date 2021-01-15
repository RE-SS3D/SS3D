using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using SS3D.Engine.Tiles;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoaderManager : NetworkSceneChecker
{
    public static SceneLoaderManager singleton { get; private set; }
    
    // MAPS IN BUILD
    [SerializeField] private String selectedMap;

    [SerializeField] private String[] maps;
    
    [SerializeField] private Button startRoundButton;
    [SerializeField] private TMP_Text loadSceneButtonText;

    [SerializeField] private TMP_Dropdown mapSelectionDropdown;
    
    private void Start()
    {
        if (singleton != null) Destroy(gameObject);
        singleton = this;

        LoadMapList();
    }

    public void LoadMapScene()
    {
        if (IsSelectedMapLoaded()) return;
    
        loadSceneButtonText.text = "loading...";
        LoadingSceneHelper();
        
        SceneManager.LoadSceneAsync(selectedMap, LoadSceneMode.Additive);
        SceneManager.sceneLoaded += SetActiveScene;
        Debug.Log("New active scene set " + GetCurrentLoadedScene().name);

        TileManager.tileManagerLoaded += UnlockRoundStart;
    }

    public void UnlockRoundStart()
    {
        loadSceneButtonText.text = "scene loaded";
        startRoundButton.interactable = true;
        
        SceneMessage msg = new SceneMessage
        {
            sceneName = selectedMap,
            sceneOperation = SceneOperation.LoadAdditive
        };

        NetworkServer.SendToAll(msg);
    }
    public void LoadMapList()
    {
        List<TMP_Dropdown.OptionData> mapList = new List<TMP_Dropdown.OptionData>();

        foreach (String map in maps)
        {
            TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(map);
            mapList.Add(option);
        }
        mapSelectionDropdown.options = mapList;
        selectedMap = mapList[0].text;
        
        RpcLoadMapList();
    }

    [ClientRpc]
    private void RpcLoadMapList()
    {
        List<TMP_Dropdown.OptionData> mapList = new List<TMP_Dropdown.OptionData>();

        foreach (String map in maps)
        {
            TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(map);
            mapList.Add(option);
        }
        mapSelectionDropdown.options = mapList;
        selectedMap = mapList[0].text;
    }
    public IEnumerator LoadingSceneHelper()
    {
        yield return new WaitUntil(IsSelectedMapLoaded);
    }
    
    public bool IsSelectedMapLoaded()
    {
        if (SceneManager.GetSceneByName(selectedMap).isLoaded) return true;
        return false;
    }

    public void UnloadSelectedMap()
    {
        SceneManager.UnloadSceneAsync(selectedMap);
    }
    
    public void SetSelectedMap(TMP_Dropdown dropdown)
    {
        String name = dropdown.captionText.text;
        loadSceneButtonText.text = "load map";
        
        if (IsSelectedMapLoaded() && selectedMap == name) return;
        
        if (IsSelectedMapLoaded())
            UnloadSelectedMap();
            
        foreach (String map in maps)
        {
            if (map == name) selectedMap = map;
        }
    }

    public void SetActiveScene(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Setting new active scene: " + scene.name);
        if (scene.name == selectedMap)
            SceneManager.SetActiveScene(GetCurrentLoadedScene());
    }
    
    public Scene GetCurrentLoadedScene()
    {
        return SceneManager.GetSceneByName(selectedMap);
    }
}
