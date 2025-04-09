using SS3D.Systems.Tile.UI;

namespace SS3D.Systems.Tile.TileMapCreator
{
    public class ConstructionSlot : AssetSlot
    {
        private ConstructionHologramManager _hologramManager;
        
        public void Setup(GenericObjectSo genericObjectSo)
        {
            base.Setup(genericObjectSo);
            _hologramManager = GetComponentInParent<ConstructionHologramManager>(); 
        }
        
        public void OnClick()
        {
            _hologramManager.SetSelectedObject(GenericObjectSo);
        }
    }
}