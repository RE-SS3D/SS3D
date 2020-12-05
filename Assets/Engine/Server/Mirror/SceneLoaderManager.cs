using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
using Mirror;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoaderManager : NetworkSceneChecker
{
    public static SceneLoaderManager singleton { get; private set; }
    // MAPS
    [SerializeField] private SceneAsset selectedMap;
    [SerializeField] private SceneAsset[] maps;

    private void Start()
    {
        if (singleton != null) Destroy(gameObject);
        singleton = this;
    }

    public void LoadMapScene()
    {
        SceneManager.LoadSceneAsync(selectedMap.name, LoadSceneMode.Additive);
        
        SceneManager.sceneLoaded += SetActiveScene;
        Debug.Log("New active scene set " + GetCurrentLoadedScene().name);
        
        SceneMessage msg = new SceneMessage
        {
            sceneName = selectedMap.name,
            sceneOperation = SceneOperation.LoadAdditive
        };

        if (connectionToClient != null)
            connectionToClient.Send(msg);
    }
    
    
    public bool IsSelectedMapLoaded()
    {
        if (SceneManager.GetSceneByName(selectedMap.name) != null) return true;
        else return false;
    }

    public void SetActiveScene(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == selectedMap.name)
            SceneManager.SetActiveScene(GetCurrentLoadedScene());
    }
    
    
    public Scene GetCurrentLoadedScene()
    {
        return SceneManager.GetSceneByName(selectedMap.name);
    }
}
