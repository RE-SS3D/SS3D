using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Inventory.UI
{
    public struct ContainerDisplay
    {
        public GameObject UiElement;
        public ContainerDescriptor Container;

        public ContainerDisplay(GameObject uiElement, ContainerDescriptor container)
        {
            UiElement = uiElement;
            Container = container;
        }
    }
}