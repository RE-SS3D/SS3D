using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoaderManager : NetworkSceneChecker
{
    // MAPS
    [SerializeField] private SceneAsset selectedMap;
    [SerializeField] private SceneAsset[] maps;

    public void LoadMapScene()
    {
        SceneManager.LoadSceneAsync(selectedMap.name, LoadSceneMode.Additive);  
        
        SceneMessage msg = new SceneMessage
        {
            sceneName = selectedMap.name,
            sceneOperation = SceneOperation.LoadAdditive
        };

        connectionToClient.Send(msg);
    }
}
