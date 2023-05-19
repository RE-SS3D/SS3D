using Coimbra;

namespace SS3D.Systems.Inventory.UI
{
    public class ItemGridItem : ItemDisplay
    {
        public override void OnDropAccepted()
        {
            base.OnDropAccepted();
            (InventoryDisplayElement as ItemGridAlt)?.RemoveGridItem(this);
            gameObject.Dispose(true);
        }
    }
}