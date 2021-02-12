using SS3D.Engine.Inventory;
using SS3D.Content.Furniture;

namespace SS3D.Content.Items.Cosmetic
{
    public class FloorSignOpener : Openable
    {
        private bool lastFrameInContainer;
        private Item item;

        public override void Start()
        {
            base.Start();
            item = GetComponent<Item>();

            lastFrameInContainer = item.InContainer();
            UpdateStatus(lastFrameInContainer);
        }

        private void Update()
        {
            bool newInContainer = item.InContainer();
            if (lastFrameInContainer != newInContainer)
            {
                UpdateStatus(newInContainer);
            }
            lastFrameInContainer = newInContainer;
        }

        private void UpdateStatus(bool inContainer)
        {
            SetOpen(!inContainer);
        }
    }
}