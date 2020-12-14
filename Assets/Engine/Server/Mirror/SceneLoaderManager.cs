using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoaderManager : NetworkSceneChecker
{
    public static SceneLoaderManager singleton { get; private set; }
    // MAPS
    [SerializeField] private SceneAsset selectedMap;
    [SerializeField] private SceneAsset[] maps;
    
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
        if (IsSelectedMapLoaded())
    
        loadSceneButtonText.text = "loading...";
        LoadingSceneHelper();
        
        SceneManager.LoadSceneAsync(selectedMap.name, LoadSceneMode.Additive);
        SceneManager.sceneLoaded += SetActiveScene;
        Debug.Log("New active scene set " + GetCurrentLoadedScene().name);
        loadSceneButtonText.text = "scene loaded";
        startRoundButton.interactable = true;
        
        SceneMessage msg = new SceneMessage
        {
            sceneName = selectedMap.name,
            sceneOperation = SceneOperation.LoadAdditive
        };

        if (connectionToClient != null)
            connectionToClient.Send(msg);
    }

    public void LoadMapList()
    {
        List<TMP_Dropdown.OptionData> mapList = new List<TMP_Dropdown.OptionData>();

        foreach (SceneAsset map in maps)
        {
            TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(map.name);
            mapList.Add(option);
        }
        mapSelectionDropdown.options = mapList;
    }
    public IEnumerator LoadingSceneHelper()
    {
        yield return new WaitUntil(IsSelectedMapLoaded);
    }
    
    public bool IsSelectedMapLoaded()
    {
        if (SceneManager.GetSceneByName(selectedMap.name).isLoaded) return true;
        return false;
    }

    public void UnloadSelectedMap()
    {
        SceneManager.UnloadSceneAsync(selectedMap.name);
    }
    
    public void SetSelectedMap(TMP_Dropdown dropdown)
    {
        String name = dropdown.captionText.text;
        loadSceneButtonText.text = "load map";
        
        if (IsSelectedMapLoaded() && selectedMap.name == name) return;
        
        if (IsSelectedMapLoaded())
            UnloadSelectedMap();
            
        foreach (SceneAsset map in maps)
        {
            if (map.name == name) selectedMap = map;
        }
    }

    public void SetActiveScene(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Setting new active scene: " + scene.name);
        if (scene.name == selectedMap.name)
            SceneManager.SetActiveScene(GetCurrentLoadedScene());
    }
    
    public Scene GetCurrentLoadedScene()
    {
        return SceneManager.GetSceneByName(selectedMap.name);
    }
}
