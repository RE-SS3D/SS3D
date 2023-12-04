using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data.Management;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SS3D.Systems.Tile.TileMapCreator
{
    public class TileMapSaveTab : NetworkActor, ITileMenuTab
    {
        [SerializeField]
        private TileMapMenu _menu;

        [SerializeField]
        private GameObject _confirmOverWriteButton;

        [SerializeField]
        private TMP_InputField _saveInputField;

        [SerializeField]
        private GameObject _saveMapContentRoot;

        public void Clear()
        {
            _confirmOverWriteButton.gameObject.SetActive(false);
            _saveMapContentRoot.SetActive(false);
        }

        public void Display()
        {
            _saveMapContentRoot.gameObject.SetActive(true);
            _confirmOverWriteButton.gameObject.SetActive(false);
            _saveMapContentRoot.SetActive(true);
        }

        public void Refresh()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Method called when the save button is clicked.
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
            if (AlreadyContainsName(_saveInputField.text))
            {
                _confirmOverWriteButton.gameObject.SetActive(true);
            }
            else
            {
                SaveMap(_saveInputField.text);
            }
        }

        public bool AlreadyContainsName(string name)
        {
            string savePath = Subsystems.Get<TileSystem>().SavePath;
            List<string> saves = SaveSystem.GetAllObjectsNameInFolder(savePath);
            return saves.Contains(name + ".json");
        }

        public void SaveMap(string mapName)
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
