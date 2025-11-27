using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Inventory.UI
{
    public struct ContainerDisplay
    {
        public GameObject UiElement;
        public AttachedContainer Container;

        public ContainerDisplay(GameObject uiElement, AttachedContainer container)
        {
            UiElement = uiElement;
            Container = container;
        }
    }
}
