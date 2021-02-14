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
    /// Scene loader manager, also manages admin settings UI
    /// </summary>
    public class SceneLoaderManager : NetworkBehaviour
    {
        public static SceneLoaderManager singleton { get; private set; }

        public static event System.Action mapLoaded;
        // MAPS IN BUILD
        [SerializeField] [SyncVar] private String selectedMap;

        [SerializeField] private String[] maps;

        [SerializeField] private Image startRoundImage;
        [SerializeField] private Button startRoundButton;
        [SerializeField] private TMP_Text startRoundButtonText;
        
        [SerializeField] private TMP_Text loadSceneButtonText;
        [SerializeField] private Button loadSceneButton;

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

        public void LoadMapScene()
        {
            if (IsSelectedMapLoaded()) return;

            loadSceneButtonText.text = "loading...";
            
            mapLoaded?.Invoke();
            RpcInvokeMapLoaded();

            SceneManager.LoadSceneAsync(selectedMap, LoadSceneMode.Additive);
            StartCoroutine(SetActiveScene(selectedMap));

            TileManager.tileManagerLoaded += UnlockRoundStart;
        }

        [ClientRpc]
        private void RpcInvokeMapLoaded()
        {
            if (isServer) return;
            mapLoaded?.Invoke();
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

        public void UnloadSelectedMap()
        {
            SceneManager.UnloadSceneAsync(selectedMap);

            // just in case (Restarts for example)
            loadSceneButtonText.text = "load map";
            loadSceneButton.interactable = true;
            startRoundButton.interactable = false;

            SceneMessage msg = new SceneMessage
            {
                sceneName = selectedMap,
                sceneOperation = SceneOperation.UnloadAdditive
            };

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

        [Command(ignoreAuthority = true)]
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
