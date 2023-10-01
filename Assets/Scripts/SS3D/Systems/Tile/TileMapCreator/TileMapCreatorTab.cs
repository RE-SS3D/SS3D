using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Systems.Tile.UI
{
    /// <summary>
    /// Tab that holds information for each item/tile in the TileMapCreator UI.
    /// </summary>
    public class TileMapCreatorTab : MonoBehaviour
    {
        private Image _image;
        private GenericObjectSo _genericObjectSo;
        private TileMapCreator.TileMapCreator _menu;
        

        /// <summary>
        /// Load a new UI icon and string for the item/tile.
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="nameString"></param>
        private void LoadTab(Sprite icon, string nameString)
        {
            _image = GetComponent<Image>();
            _image.sprite = icon;
            transform.localScale = Vector3.one;

            GetComponentInChildren<TMP_Text>().text = nameString;
            _menu = GetComponentInParent<TileMapCreator.TileMapCreator>();
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        public void Setup(GenericObjectSo genericObjectSo)
        {
            _genericObjectSo = genericObjectSo;

            LoadTab(genericObjectSo.icon, genericObjectSo.nameString);
        }

        public void OnClick()
        {
            if (_genericObjectSo != null)
                _menu.SetSelectedObject(_genericObjectSo);
        }
    }
}