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
        [SerializeField]
        protected Image Image;
        [SerializeField]
        protected TMP_Text AssetName;
        protected GenericObjectSo GenericObjectSo;
        
        /// <summary>
        /// Load an UI icon and string for the item/tile.
        /// </summary>
        /// <param name="genericObjectSo"></param>
        public void Setup(GenericObjectSo genericObjectSo)
        {
            GenericObjectSo = genericObjectSo;
            Image.sprite = genericObjectSo.icon;
            AssetName.text = genericObjectSo.NameString;
        }
    }
}