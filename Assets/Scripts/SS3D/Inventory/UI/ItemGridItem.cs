using Coimbra;
using SS3D.Engine.Inventory.UI;

namespace SS3D.Inventory.UI
{
    public class ItemGridItem : ItemDisplay
    {
        public override void OnDropAccepted()
        {
            base.OnDropAccepted();
            (InventoryDisplayElement as ItemGrid)?.RemoveGridItem(this);
            gameObject.Destroy();
        }
    }
}