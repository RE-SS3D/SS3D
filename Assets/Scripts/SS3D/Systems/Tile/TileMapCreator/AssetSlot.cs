using SS3D.Systems.Tile.TileMapCreator;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Systems.Tile.UI
{
    /// <summary>
    /// Slot that holds information for each item/tile in the TileMapCreator UI.
    /// They get created when the tilemap menu spawns.
    /// </summary>
    public class AssetSlot : MonoBehaviour
    {
        private GenericObjectSo _genericObjectSo;

        private ConstructionHologramManager _hologramManager;
        /// <summary>
        /// Load an UI icon and string for the item/tile.
        /// </summary>
        /// <param name="genericObjectSo"></param>
        public void Setup(GenericObjectSo genericObjectSo)
        {
            _genericObjectSo = genericObjectSo;
            GetComponent<Image>().sprite = genericObjectSo.icon;
            transform.localScale = Vector3.one;
            GetComponentInChildren<TMP_Text>().text = genericObjectSo.NameString;

            _hologramManager = GetComponentInParent<ConstructionHologramManager>(); 
        }

        public void OnClick()
        {
            _hologramManager.SetSelectedObject(_genericObjectSo);
        }
    }
}