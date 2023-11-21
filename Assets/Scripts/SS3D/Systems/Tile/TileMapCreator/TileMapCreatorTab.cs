using SS3D.Systems.Tile.TileMapCreator;
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
        private GenericObjectSo _genericObjectSo;
        private TileMapCreator.TileMapCreator _menu;
        private BuildGhostManager _ghostManager;
        /// <summary>
        /// Load an UI icon and string for the item/tile.
        /// </summary>
        /// <param name="genericObjectSo"></param>
        public void Setup(GenericObjectSo genericObjectSo)
        {
            _genericObjectSo = genericObjectSo;
            GetComponent<Image>().sprite = genericObjectSo.icon;
            transform.localScale = Vector3.one;
            GetComponentInChildren<TMP_Text>().text = genericObjectSo.nameString;
            _menu = GetComponentInParent<TileMapCreator.TileMapCreator>();
            _ghostManager = GetComponentInParent<BuildGhostManager>(); 
        }

        public void OnClick()
        {
            _ghostManager.SetSelectedObject(_genericObjectSo);
        }
    }
}