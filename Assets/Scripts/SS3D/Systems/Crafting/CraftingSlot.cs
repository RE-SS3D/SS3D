using SS3D.Systems.Tile;
using SS3D.Systems.Tile.UI;
using TMPro;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    public class CraftingSlot : AssetSlot
    {
        [SerializeField]
        private TMP_Text _textAmount;
        public void Setup(GenericObjectSo genericObjectSo, uint amount)
        {
            base.Setup(genericObjectSo);
            _textAmount.text = "x" + amount;
        }
    }
}