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
    /// Script to handle displaying the UI for the saving tab of the tilemap menu.
    /// Also handle the logic of saving maps.
    /// </summary>
    public class TileMapSaveTab : NetworkActor, ITileMenuTab
    {
        [SerializeField]
        private TileMapMenu _menu;

        [SerializeField]
        private GameObject _confirmOverWriteButton;

        [SerializeField]
        private ControlsOffInputField _saveInputField;

        [SerializeField]
        private GameObject _saveMapContentRoot;

        /// <summary>
        /// Clear the save tab.
        /// </summary>
        public void Clear()
        {
            _confirmOverWriteButton.gameObject.SetActive(false);
            _saveMapContentRoot.SetActive(false);
        }

        /// <summary>
        /// Display the save tab.
        /// </summary>
        public void Display()
        {
            _saveMapContentRoot.gameObject.SetActive(true);
            _confirmOverWriteButton.gameObject.SetActive(false);
            _saveMapContentRoot.SetActive(true);
        }

        /// <summary>
        /// Refresh the save tab.
        /// </summary>
        public void Refresh()
        {
            Clear();
            Display();
        }

        /// <summary>
        /// Method called when the confirm overwrite button is clicked.
        /// </summary>
        public void HandleConfirmOverWriteButton()
        {
            SaveMap(_saveInputField.text);
        }

        /// <summary>
        /// Method called when the save button is clicked.
        /// </summary>
        public void HandleSaveMapButton()
        {
            if (Subsystems.Get<TileSystem>().MapNameAlreadyExist(_saveInputField.text))
            {
                _confirmOverWriteButton.gameObject.SetActive(true);
            }
            else
            {
                SaveMap(_saveInputField.text);
            }
        }

        /// <summary>
        /// Save a map, with name mapName
        /// </summary>
        /// <param name="mapName"></param>
        private void SaveMap(string mapName)
        {
            if (IsServer)
            {
                Subsystems.Get<TileSystem>().Save(mapName, true);
            }
            else
            {
                Log.Information(this, "Cannot save the map on a client");
            }
        }
    }
}
