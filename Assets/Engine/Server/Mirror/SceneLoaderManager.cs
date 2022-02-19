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
        [Scene]
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

        public bool CommencedLoadingMap { get; private set; }

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
        }

        private void Start()
        {
            // Run LoadMapList() to populate the map list on the server.
            if (isServer)
            {
                LoadMapList();
            }
        }

	// Here we load the selected map
        public void LoadMapScene()
        {
            // Only allow the administrator to click this button once!
            if (CommencedLoadingMap) return;

            CommencedLoadingMap = true;
            loadSceneButtonText.text = "loading...";
            
            mapLoaded?.Invoke();
            RpcInvokeMapLoaded();

            SceneManager.LoadSceneAsync(selectedMap, LoadSceneMode.Additive);
            StartCoroutine(SetActiveScene(selectedMap));

            TileManager.TileManagerLoaded += UnlockRoundStart;
        }

        [ClientRpc]
        private void RpcInvokeMapLoaded()
        {
            if (isServer) return;
            mapLoaded?.Invoke();
        }

        [TargetRpc]
        public void TargetInvokeMapLoaded(NetworkConnection conn)
        {
            if (isServer) return;
            mapLoaded?.Invoke();
        }

	// Unlock round start when the map is loaded, also sends the client the scene to load for some reason
	// I'll move it to another place
	// TODO: move the scene message to LoadMapScene()
        public void UnlockRoundStart()
        {
	    // UI stuff
            loadSceneButtonText.text = "scene loaded";
            startRoundButton.interactable = true;

	    // creates a message for the clients that tell them to load the selected scene
            SceneMessage msg = GenerateSceneMessage();

	    // sends said message to all clients
            NetworkServer.SendToAll(msg);
        }

        public SceneMessage GenerateSceneMessage()
        {
            SceneMessage msg = new SceneMessage
            {
                sceneName = selectedMap,
                sceneOperation = SceneOperation.LoadAdditive
            };
            return msg;
        }

	// Updates map list in the dropdown using the map list
        public void LoadMapList()
        {
            // Prevent map list from loading on clients.
            if (!isServer) return;

            List<TMP_Dropdown.OptionData> mapList = new List<TMP_Dropdown.OptionData>();

            foreach (string map in maps)
            {
                TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(map);
                mapList.Add(option);
            }

            mapSelectionDropdown.options = mapList;
            selectedMap = mapList[0].text;
            
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

        [TargetRpc]
        public void TargetSetActiveScene(NetworkConnection conn)
        {
            if (isServer) return;

            Debug.Log("Setting new active scene: " + selectedMap);
            SceneManager.SetActiveScene(GetSelectedScene());
            Debug.Log("New active scene set " + SceneManager.GetActiveScene().name);

            TileManager.Instance.Reinitialize();

            if (RoundManager.singleton.IsRoundStarted)
            {
                ServerLobbyUIHelper.singleton.ChangeEmbarkText();
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
