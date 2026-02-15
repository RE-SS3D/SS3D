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
        private TileMapMenuSubSystem _menu;

        [SerializeField]
        private GameObject _confirmOverWriteButton;

        [SerializeField]
        private ControlsOffInputField _saveInputField;

        [SerializeField]
        private GameObject _saveMapContentRoot;

        [SerializeField]
        private TextMeshProUGUI _saveText;

        [SerializeField]
        private float _fadeDuration;

        /// <summary>
        /// Clear the save tab.
        /// </summary>
        public void Clear()
        {
            _confirmOverWriteButton.gameObject.SetActive(false);
            _saveMapContentRoot.SetActive(false);
            _saveText.gameObject.SetActive(false);
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
            _confirmOverWriteButton.gameObject.SetActive(false);
        }

        /// <summary>
        /// Method called when the save button is clicked.
        /// </summary>
        public void HandleSaveMapButton()
        {
            if (SubSystems.Get<TileSubSystem>().MapNameAlreadyExist(_saveInputField.text))
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
                SubSystems.Get<TileSubSystem>().Save(mapName, true);
                DisplaySaveText(mapName);
            }
            else
            {
                Log.Information(this, "Cannot save the map on a client");
            }
        }

        /// <summary>
        /// Display some text to inform player the map is saved.
        /// </summary>
        /// <param name="mapName"> name of the map.</param>
        private void DisplaySaveText(string mapName)
        {
            _saveText.text = "map " + mapName + " saved !";
            _saveText.gameObject.SetActive(true);
            StartCoroutine(FadeText());
        }

        private IEnumerator FadeText()
        {
            Color originalColor = _saveText.color;

            float currentTime = 0f;

            while (currentTime < _fadeDuration)
            {
                currentTime += Time.deltaTime;

                // Calculate the alpha value based on the current time and duration
                float alpha = Mathf.Lerp(1f, 0f, currentTime / _fadeDuration);

                // Set the text's alpha to the calculated value
                _saveText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

                yield return null;
            }

            _saveText.gameObject.SetActive(false);
        }


    }
}
