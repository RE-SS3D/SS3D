using Coimbra;
using SS3D.Core.Behaviours;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SS3D.Systems.Tile.TileMapCreator
{
    public class TileMapBuildTab : NetworkActor, ITileMenuTab
    {
        [SerializeField]
        private TileMapMenu _menu;

        [SerializeField]
        private GameObject _contentRoot;

        /// <summary>
        /// Input field to search for specific tile objects or items in the menu.
        /// </summary>
        [SerializeField]
        private TMP_InputField _tileObjectSearchBar;


        /// <summary>
        /// Dropdown to select the layer to display in the menu.
        /// </summary>
        [SerializeField]
        private TMP_Dropdown _layerPlacementDropdown;

        [SerializeField]
        private AssetGrid _assetGrid;

        public void Clear()
        {
            _tileObjectSearchBar.gameObject.SetActive(false);
            _layerPlacementDropdown.gameObject.SetActive(false);


            for (int i = 0; i < _contentRoot.transform.childCount; i++)
            {
                _contentRoot.transform.GetChild(i).gameObject.Dispose(true);
            }
        }

        public void Display()
        {
            _assetGrid.Setup();
            _tileObjectSearchBar.gameObject.SetActive(true);
            _layerPlacementDropdown.gameObject.SetActive(true);
        }

        public void Refresh()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Called when the text in the input field to search for tile objects is changed.
        /// </summary>
        public void HandleInputFieldChanged()
        {
            _assetGrid.FindAssets(_tileObjectSearchBar.text);
        }
    }
}
