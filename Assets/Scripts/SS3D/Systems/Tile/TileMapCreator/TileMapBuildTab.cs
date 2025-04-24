using Coimbra;
using SS3D.Core.Behaviours;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// Script to handle displaying the UI for the build tab of the tilemap menu.
    /// Handle searching for tile objects, switching between deleting and building, selecting which
    /// object to build.
    /// </summary>
    public class TileMapBuildTab : NetworkActor, ITileMenuTab
    {
        [SerializeField]
        private TileMapMenuSubSystem _menu;

        /// <summary>
        /// Game object for the root of slots of the build tab.
        /// </summary>
        [SerializeField]
        private GameObject _slotsRoot;

        /// <summary>
        /// Game object for the root of the content of the build tab.
        /// </summary>
        [SerializeField]
        private GameObject _buildRoot;

        /// <summary>
        /// Input field to search for specific tile objects or items in the menu.
        /// </summary>
        [SerializeField]
        private ControlsOffInputField _tileObjectSearchBar;

        /// <summary>
        /// Dropdown to select the layer to display in the menu.
        /// </summary>
        [SerializeField]
        private TMP_Dropdown _layerPlacementDropdown;

        [SerializeField]
        private AssetGrid _assetGrid;

        /// <summary>
        /// Button to switch between building and deleting mode.
        /// </summary>
        [SerializeField]
        private Button _buildOrDelete;

        /// <summary>
        /// true if the construction mode is deleting the tile objects.
        /// </summary>
        private bool _isDeleting;
        public bool IsDeleting => _isDeleting;

        // purpulish color when deleting
        private Color _deleteColor = new Color(169/255f, 102/255f, 223/255f, 1f);

        // bluish color when building
        private Color _buildColor = new Color(88/255f, 148/255f, 223/255f, 1f);


        /// <summary>
        /// Clear the build tab.
        /// </summary>
        public void Clear()
        {
            _buildRoot.gameObject.SetActive(false);
            _tileObjectSearchBar.gameObject.SetActive(false);
            _layerPlacementDropdown.gameObject.SetActive(false);
            _buildOrDelete.gameObject.SetActive(false);
            _tileObjectSearchBar.text = string.Empty;


            for (int i = 0; i < _slotsRoot.transform.childCount; i++)
            {
                _slotsRoot.transform.GetChild(i).gameObject.Dispose(true);
            }
        }

        /// <summary>
        /// Display all elements of the building tab.
        /// </summary>
        public void Display()
        {
            _assetGrid.Setup();
            _tileObjectSearchBar.text = string.Empty;
            _buildRoot.gameObject.SetActive(true);
            _tileObjectSearchBar.gameObject.SetActive(true);
            _layerPlacementDropdown.gameObject.SetActive(true);
            _buildOrDelete.gameObject.SetActive(true);
        }

        /// <summary>
        /// Refresh the build tab.
        /// </summary>
        public void Refresh()
        {
            Clear();
            Display();
        }

        /// <summary>
        /// Called when the text in the input field to search for tile objects is changed.
        /// </summary>
        public void HandleInputFieldChanged()
        {
            _assetGrid.FindAssets(_tileObjectSearchBar.text);
        }

        /// <summary>
        /// Called when the build or delete button is pressed.
        /// </summary>
        public void HandleBuildOrDeleteButton()
        {
            _isDeleting = !_isDeleting;
            var tmpComponent = (TextMeshProUGUI)_buildOrDelete.targetGraphic;
            if (IsDeleting)
            {
                tmpComponent.text = "Delete";
                tmpComponent.color = _deleteColor;
            }
            else
            {
                tmpComponent.text = "Build";
                tmpComponent.color = _buildColor;
            }
        }
    }
}
