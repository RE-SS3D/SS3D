using Coimbra;

namespace SS3D.Storage.UI
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