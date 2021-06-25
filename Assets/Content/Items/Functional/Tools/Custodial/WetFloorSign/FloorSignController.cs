using SS3D.Engine.Inventory;
using SS3D.Content.Furniture;

namespace SS3D.Content.Items.Cosmetic
{
    // This needs some rework, not sure if I want that code on Update
    public class FloorSignController : Openable
    {
        private bool lastInContainer;
        private Item item;

        public override void Start()
        {
            base.Start();
            item = GetComponent<Item>();

            OnContainerUpdate(item.InContainer());
        }

        private void Update()
        {
            bool newInContainer = item.InContainer();
            if (lastInContainer != newInContainer)
            {
                OnContainerUpdate(newInContainer);
            }
        }

        private void OnContainerUpdate(bool inContainer)
        {
            SetOpen(!inContainer);
            lastInContainer = inContainer;
        }
    }
}