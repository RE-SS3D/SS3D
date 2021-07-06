using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using SS3D.Engine.Server.Round;
using SS3D.Engine.Tiles;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SS3D
{
    /// <summary>
    /// This handles the scene loading and unloading, also manages admin settings UI
    /// </summary>
    public class SceneLoaderManager : NetworkBehaviour
    {
        public static SceneLoaderManager singleton { get; private set; }

	// Fires when a map is loaded
        public static event System.Action mapLoaded;

        // Select map that will be loaded or is loaded
        [SerializeField] [SyncVar] private String selectedMap;

	// All the maps that are possible to be loaded
	// its a string because Unity is special
        [SerializeField] private String[] maps;

	// I do not recall what this does
        [SerializeField] private Image startRoundImage;
        [SerializeField] private Button startRoundButton;
        [SerializeField] private TMP_Text startRoundButtonText;
        
	// the "load" button in the server admin panel, in the settings tab
        [SerializeField] private TMP_Text loadSceneButtonText;
        [SerializeField] private Button loadSceneButton;

	// the dropdown that should be with the possible maps
        [SerializeField] private TMP_Dropdown mapSelectionDropdown;

        private void Awake()
        {
            if (singleton != null && singleton != this)
            {
                Destroy(gameObject);
            }
            else
            {
                singleton = this;
            }

            // perish
            RoundManager.ServerRoundEnded += delegate
            {
                startRoundButtonText.text = "start round";
                startRoundImage.color = MaterialChanger.GetColor(MaterialChanger.Palette01.green);
            };

            startRoundButton.onClick.AddListener(delegate { HandleRoundButton(); });
            LoadMapList();
        }

	    // Here we load the selected map
        [Server]
        public void LoadMapScene()
        {

            // Entry validation
            if (string.IsNullOrEmpty(selectedMap))
            {
                Debug.LogWarning("LoadMapScene: empty scene name. Aborting.");
                return;
            }

            if (IsSelectedMapLoaded())
            {
                Debug.LogWarning("LoadMapScene: scene " + selectedMap + " is already loaded. Aborting.");
                return;
            }

            // UI changes. mapLoaded event causes animators to fire.
            loadSceneButtonText.text = "loading...";
            mapLoaded?.Invoke();
            RpcInvokeMapLoaded();

            Debug.Log("LoadMapScene: " + selectedMap);
            NetworkServer.SetAllClientsNotReady();

            // Let server prepare for scene change. We will set the networkSceneName in OnServerChangeScene.
            LoginNetworkManager.singleton.OnServerChangeScene(selectedMap);

            // Suspend the server's transport while changing scenes
            // It will be re-enabled in FinishLoadScene.
            Transport.activeTransport.enabled = false;

            LoginNetworkManager.loadingSceneAsync = SceneManager.LoadSceneAsync(selectedMap, LoadSceneMode.Additive);

            // ServerChangeScene can be called when stopping the server
            // when this happens the server is not active so does not need to tell clients about the change
            if (NetworkServer.active)
            {
                // notify all clients about the new scene
                NetworkServer.SendToAll(new SceneMessage { sceneName = selectedMap, sceneOperation = SceneOperation.LoadAdditive });
            }

            //startPositionIndex = 0;
            //startPositions.Clear();

            //StartCoroutine(SetActiveScene(selectedMap));

            // Once TileManager has done its validation, unlock the Round Start button.
            TileManager.tileManagerLoaded += UnlockRoundStart;
        }

        [ClientRpc]
        private void RpcInvokeMapLoaded()
        {
            if (isServer) return;
            mapLoaded?.Invoke();
        }

	// Unlock round start when the map is loaded
        public void UnlockRoundStart()
        {
	    // UI stuff
            loadSceneButtonText.text = "scene loaded";
            startRoundButton.interactable = true;

        }

	// Updates map list in the dropdown using the map list
	// TODO: Update the map list via Server
        public void LoadMapList()
        {
            List<TMP_Dropdown.OptionData> mapList = new List<TMP_Dropdown.OptionData>();

            foreach (string map in maps)
            {
                TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(map);
                mapList.Add(option);
            }

            mapSelectionDropdown.options = mapList;
            selectedMap = mapList[0].text;
            
            RpcLoadMapList(maps);
        }

        [ClientRpc]
        private void RpcLoadMapList(string[] mapList)
        {
            maps = mapList;
            selectedMap = mapList[0];
        }

        public bool IsSelectedMapLoaded()
        {
            if (SceneManager.GetSceneByName(selectedMap).isLoaded) return true;
            return false;
        }

	
	// Unloads the current selected map
        public void UnloadSelectedMap()
        {
	    // Unloads the map
            SceneManager.UnloadSceneAsync(selectedMap);

            // just in case (Restarts for example)
            loadSceneButtonText.text = "load map";
            loadSceneButton.interactable = true;
            startRoundButton.interactable = false;
		
	    // Creates a message to tell clients what scene to unload
            SceneMessage msg = new SceneMessage
            {
                sceneName = selectedMap,
                sceneOperation = SceneOperation.UnloadAdditive
            };
	    
	    // Sends the client the message
            NetworkServer.SendToAll(msg);
        }

        
        public void SetSelectedMap(TMP_Dropdown dropdown)
        {
            // Gets the name from the dropdown
            String name = dropdown.captionText.text;

            // if selected map is the currently loaded map
            if (IsSelectedMapLoaded() && selectedMap == name) return;

            // if selected map is not the currently loaded map
            if (IsSelectedMapLoaded() && selectedMap != name)
            {
                if (RoundManager.singleton.IsOnWarmup || RoundManager.singleton.IsRoundStarted)
                {
                    startRoundButton.interactable = true;
                    startRoundButtonText.text = "start round";
                    startRoundImage.color = MaterialChanger.GetColor(MaterialChanger.Palette01.green);
                    RoundManager.singleton.EndRound();
                }
                
                UnloadSelectedMap();
            }

            loadSceneButtonText.text = "load map";
            foreach (String map in maps)
            {
                if (map == name) CmdSetSelectedMap(map);
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdSetSelectedMap(string scene)
        {
            selectedMap = scene;
        }
        
        public IEnumerator SetActiveScene(String scene)
        {
            yield return new WaitUntil(IsSelectedMapLoaded);
            Debug.Log("Setting new active scene: " + scene);
            if (scene == selectedMap)
            {
                SceneManager.SetActiveScene(GetSelectedScene());
                Debug.Log("New active scene set " + SceneManager.GetActiveScene().name);
                
                RpcSetActiveScene(scene);
            }
        }

        [ClientRpc]
        private void RpcSetActiveScene(String scene)
        {
            if (isServer) return;
            
            Debug.Log("Setting new active scene: " + scene);
            if (scene == selectedMap)
            {
                SceneManager.SetActiveScene(GetSelectedScene());
                Debug.Log("New active scene set " + SceneManager.GetActiveScene().name);
            }
        }

        public Scene GetSelectedScene()
        {
            return SceneManager.GetSceneByName(selectedMap);
        }

	//
        public void HandleRoundButton()
        {
            RoundManager roundManager = RoundManager.singleton;

            if (roundManager.IsOnWarmup || roundManager.IsRoundStarted)
            {
                startRoundButtonText.text = "start round";
                startRoundImage.color = MaterialChanger.GetColor(MaterialChanger.Palette01.green);
                roundManager.EndRound();
            }

            else if (!roundManager.IsRoundStarted || !roundManager.IsOnWarmup)
            {
                startRoundButtonText.text = "stop round";
                startRoundImage.color = MaterialChanger.GetColor(MaterialChanger.Palette01.red);
                roundManager.StartWarmup();
            }
        }
    }
}
