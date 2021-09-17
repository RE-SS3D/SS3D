using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SS3D.Engine.Inventory.UI;
using SS3D.Engine.Inventory;



public class ColorChangeWhenFilled : MonoBehaviour
{
    private AttachedContainer attachedContainer;
    private Image slotImage;
    // Start is called before the first frame update
    void Start()
    {
        attachedContainer =  GetComponentInParent<ContainerUi>().AttachedContainer;
        slotImage = GetComponent<Image>();
        attachedContainer.Container.ContentsChanged += OnContainerChanged;
        setColor(attachedContainer.Container);
    }

    private void OnContainerChanged(Container container, IEnumerable<IContainerizable> items,
           Container.ContainerChangeType type)
    {
        setColor(container);
    }

    private void setColor(Container container)
    {
        float percentageFull = container.AttachedTo.containerDescriptor.PercentageFull;
        if (slotImage != null)
        {
            Color slotColor = slotImage.color;
            slotColor.r = 1;
            slotColor.g = 1 - percentageFull / 100f;
            slotColor.b = 1 - percentageFull / 100f;
            slotColor.a = 0.5f + percentageFull / 200f;
            slotImage.color = slotColor;
        }
    }
}
