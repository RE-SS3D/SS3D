using SS3D.Systems.Tile;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Systems.Tile.UI
{
    /// <summary>
    /// uGUI slot displaying a tile or item asset icon and name.
    /// </summary>
    public class AssetSlot : MonoBehaviour
    {
        [SerializeField]
        protected Image Image;
        [SerializeField]
        protected TMP_Text AssetName;
        protected GenericObjectSo GenericObjectSo;

        public void Setup(GenericObjectSo genericObjectSo)
        {
            GenericObjectSo = genericObjectSo;
            Image.sprite = genericObjectSo.icon;
            AssetName.text = genericObjectSo.NameString;
        }
    }
}
