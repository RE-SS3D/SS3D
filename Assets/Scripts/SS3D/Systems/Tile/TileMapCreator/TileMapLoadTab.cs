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

                mapNameSlot.MapNameField.onSelect.AddListener(delegate { LoadMap(mapNameWithNoExtension); });
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

        /// <summary>
        /// Allow the name field to be edited, and set up listener so that at the end of edit, the name changes.
        /// </summary>
        /// <param name="mapNameField"> The input field containing the map to rename.</param>
        private void Rename(ControlsOffInputField mapNameField)
        {
            string oldName = mapNameField.text;
            mapNameField.readOnly = false;
            mapNameField.onSelect.RemoveAllListeners();
            mapNameField.onEndEdit.AddListener(delegate { RenameSave(mapNameField, oldName);});
            mapNameField.onDeselect.AddListener(delegate { RenameSave(mapNameField, oldName); });
        }


        /// <summary>
        /// Does the renaming at the end of edit, when clicking away from the input field.
        /// </summary>
        /// <param name="mapNameField">The input field containing the map to rename.</param>
        /// <param name="oldName"> The old name of the map</param>
        private void RenameSave(ControlsOffInputField mapNameField, string oldName)
        {
            mapNameField.onSelect.RemoveAllListeners();
            mapNameField.readOnly = true;

            if (Subsystems.Get<TileSystem>().MapNameAlreadyExist(mapNameField.text))
            {
                mapNameField.text = oldName;
            }
            else
            {
                string savePath = Subsystems.Get<TileSystem>().SavePath;
                SaveSystem.RenameFile(savePath + "/" + oldName, savePath + "/" + mapNameField.text);
            }

            Refresh();

            mapNameField.onSelect.AddListener(delegate { LoadMap(mapNameField.text); });
           
        }
    }
}
