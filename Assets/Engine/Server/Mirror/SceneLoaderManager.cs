using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
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
    
    private void Start()
    {
        if (singleton != null) Destroy(gameObject);
        singleton = this;
    }

    public void LoadMapScene()
    {
        if (IsSelectedMapLoaded()) return;

        loadSceneButtonText.text = "loading...";
        loadingSceneHelper();
        
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

    public IEnumerator loadingSceneHelper()
    {
        yield return new WaitUntil(IsSelectedMapLoaded);
    }
    
    public bool IsSelectedMapLoaded()
    {
        if (SceneManager.GetSceneByName(selectedMap.name).isLoaded) return true;
        return false;
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
