using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SS3D.Systems.Tile.UI
{
    /// <summary>
    /// Slot that holds information for each item/tile in the TileMapCreator UI.
    /// They get created when the tilemap menu spawns.
    /// </summary>
    public class AssetSlot : MonoBehaviour
    {
        [FormerlySerializedAs("Image")]
        [SerializeField]
        private Image _image;

        [FormerlySerializedAs("AssetName")]
        [SerializeField]
        private TMP_Text _assetName;

        protected GenericObjectSo GenericObjectSo { get; private set; }

        /// <summary>
        /// Load an UI icon and string for the item/tile.
        /// </summary>
        /// <param name="genericObjectSo"></param>
        public void Setup(GenericObjectSo genericObjectSo)
        {
            GenericObjectSo = genericObjectSo;
            _image.sprite = genericObjectSo.Icon;
            _assetName.text = genericObjectSo.NameString;
        }
    }
}
