using Coimbra;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data.Management;
using SS3D.Logging;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SS3D.Systems.Tile.TileMapCreator
{
    public class TileMapLoadTab : NetworkActor, ITileMenuTab
    {
        [SerializeField]
        private TileMapMenu _menu;

        [SerializeField]
        private GameObject _mapNameSlotPrefab;

        [SerializeField]
        private GameObject _loadMapContentRoot;

        public void Clear()
        {
            for (int i = 0; i < _loadMapContentRoot.transform.childCount; i++)
            {
                _loadMapContentRoot.transform.GetChild(i).gameObject.Dispose(true);
            }
        }

        public void Display()
        {
            var MapNames = SaveSystem.GetAllObjectsNameInFolder(Subsystems.Get<TileSystem>().SavePath);

            foreach (string mapName in MapNames)
            {
                string mapNameWithNoExtension = mapName.Substring(0, mapName.IndexOf("."));
                GameObject slot = Instantiate(_mapNameSlotPrefab, _loadMapContentRoot.transform, true);
                

                MapNameSlot mapNameSlot = slot.GetComponent<MapNameSlot>();
                mapNameSlot.MapNameField.readOnly = true;
                mapNameSlot.MapNameField.text = mapNameWithNoExtension;

                mapNameSlot.MapNameField.onSelect.AddListener((string x) => LoadMap(mapNameWithNoExtension));
                mapNameSlot.DeleteButton.onClick.AddListener(() => DeleteMap(mapNameWithNoExtension));
                mapNameSlot.DeleteButton.onClick.AddListener(() => Refresh());
                mapNameSlot.RenameButton.onClick.AddListener(() => Rename(mapNameSlot.MapNameField));
                slot.gameObject.SetActive(true);
            }
        }

        public void Refresh()
        {
            Clear();
            Display();
        }

        private void DeleteMap(string mapName)
        {
            if (IsServer)
            {
                SaveSystem.DeleteFile(Subsystems.Get<TileSystem>().SavePath + "/" + mapName);
            }
            else
            {
                Log.Information(this, "Cannot load the map on a client");
            }
        }

        private void LoadMap(string mapName)
        {
            if (IsServer)
            {
                Subsystems.Get<TileSystem>().Load(Subsystems.Get<TileSystem>().SavePath + "/" + mapName);
            }
            else
            {
                Log.Information(this, "Cannot load the map on a client");
            }
        }

        public bool AlreadyContainsName(string name)
        {
            string savePath = Subsystems.Get<TileSystem>().SavePath;
            List<string> saves = SaveSystem.GetAllObjectsNameInFolder(savePath);
            return saves.Contains(name + ".json");
        }

        private void Rename(ControlsOffInputField mapNameField)
        {
            string oldName = mapNameField.text;
            mapNameField.readOnly = false;
            mapNameField.onSelect.RemoveAllListeners();
            mapNameField.ActivateInputField();
            mapNameField.onDeselect.AddListener((x) => RenameSave(mapNameField, oldName));
            mapNameField.onEndEdit.AddListener((x) => RenameSave(mapNameField, oldName));
        }

        private void RenameSave(ControlsOffInputField mapNameField, string oldName)
        {
            mapNameField.onSelect.RemoveAllListeners();
            

            mapNameField.readOnly = true;
            if (AlreadyContainsName(mapNameField.text))
            {
                mapNameField.text = oldName;
                return;
            }
            else
            {
                string savePath = Subsystems.Get<TileSystem>().SavePath;
                SaveSystem.RenameFile(savePath + "/" + oldName, savePath + "/" + mapNameField.text);
            }

            mapNameField.onSelect.AddListener((string x) => LoadMap(mapNameField.text));

           
        }
    }
}
