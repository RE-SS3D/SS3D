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
    /// <summary>
    /// Script to handle displaying the UI for the loading tab of the tilemap menu.
    /// Also handle the logic of loading maps, removing maps and renaming them.
    /// </summary>
    public class TileMapLoadTab : NetworkActor, ITileMenuTab
    {
        [SerializeField]
        private TileMapMenuSubSystem _menu;

        /// <summary>
        /// A prefab containing an input field for the name of a map, 
        /// </summary>
        [SerializeField]
        private GameObject _mapNameSlotPrefab;

        [SerializeField]
        private GameObject _loadMapContentRoot;

        private const int FontSelectedSize = 18;
        private const int FontUnSelectedSize = 15;

        /// <summary>
        /// Clear the load tab.
        /// </summary>
        public void Clear()
        {
            _loadMapContentRoot.gameObject.SetActive(false);

            for (int i = 0; i < _loadMapContentRoot.transform.childCount; i++)
            {
                _loadMapContentRoot.transform.GetChild(i).gameObject.Dispose(true);
            }
        }

        /// <summary>
        /// Display the Load tab, including showing the names of all existing maps.
        /// </summary>
        public void Display()
        {
            _loadMapContentRoot.gameObject.SetActive(true);
            var MapNames = LocalStorage.GetAllObjectsNameInFolder(SubSystems.Get<TileSubSystem>().SavePath);

            foreach (string mapName in MapNames)
            {
                string mapNameWithNoExtension = mapName.Substring(0, mapName.IndexOf("."));
                GameObject slot = Instantiate(_mapNameSlotPrefab, _loadMapContentRoot.transform, true);

                // I've no idea why but something modify the slots scale so it's necessary to adjust it here.
                slot.transform.localScale= Vector3.one;

                MapNameSlot mapNameSlot = slot.GetComponent<MapNameSlot>();
                mapNameSlot.MapNameField.readOnly = true;
                mapNameSlot.MapNameField.text = mapNameWithNoExtension;

                mapNameSlot.MapNameField.onSelect.AddListener((string x) => LoadMap(mapNameWithNoExtension));
                mapNameSlot.DeleteButton.onClick.AddListener(() => DeleteMap(mapNameWithNoExtension));
                mapNameSlot.RenameButton.onClick.AddListener(() => Rename(mapNameSlot.MapNameField));
                slot.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Refresh the load tab.
        /// </summary>
        public void Refresh()
        {
            Clear();
            Display();
        }

        /// <summary>
        /// Delete the map with name mapName.
        /// </summary>
        /// <param name="mapName"></param>
        private void DeleteMap(string mapName)
        {
            if (IsServer)
            {
                LocalStorage.DeleteFile(SubSystems.Get<TileSubSystem>().SavePath + "/" + mapName);
            }
            else
            {
                Log.Information(this, "Cannot load the map on a client");
            }

            Refresh();
        }

        /// <summary>
        /// Load the map with name mapName
        /// </summary>
        /// <param name="mapName"></param>
        private void LoadMap(string mapName)
        {
            if (IsServer)
            {
                SubSystems.Get<TileSubSystem>().Load(SubSystems.Get<TileSubSystem>().SavePath + "/" + mapName);
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
            mapNameField.ActivateInputField();
            mapNameField.onEndEdit.AddListener((string x) => RenameSave(mapNameField, oldName));
            mapNameField.onDeselect.AddListener((string x) => RenameSave(mapNameField, oldName));
            mapNameField.textComponent.fontSize = FontSelectedSize;
        }


        /// <summary>
        /// Does the renaming at the end of edit, when clicking away from the input field.
        /// </summary>
        /// <param name="mapNameField">The input field containing the map to rename.</param>
        /// <param name="oldName"> The old name of the map</param>
        private void RenameSave(ControlsOffInputField mapNameField, string oldName)
        {
            mapNameField.textComponent.fontSize = FontUnSelectedSize;

            mapNameField.onSelect.RemoveAllListeners();
            mapNameField.readOnly = true;

            if (SubSystems.Get<TileSubSystem>().MapNameAlreadyExist(mapNameField.text))
            {
                mapNameField.text = oldName;
            }
            else
            {
                string savePath = SubSystems.Get<TileSubSystem>().SavePath;
                LocalStorage.RenameFile(savePath + "/" + oldName, savePath + "/" + mapNameField.text);
            }

            Refresh();

            mapNameField.onSelect.AddListener(delegate { LoadMap(mapNameField.text); });
           
        }
    }
}
