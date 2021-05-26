using SS3D.Engine.Inventory;
using UnityEngine;

[RequireComponent(typeof(VisibleContainer))]
public class ItemContainer : Container
{
    /* This class solely exists cause Items always need the Visible Container to work properly
     * But there are objects that have the Container class but aren't necessarily items
     * so they might not need VisibleContainer to work, this class exists just to prevent
     * people from creating container items and forgetting to put a Visible Container Component
     */
}
