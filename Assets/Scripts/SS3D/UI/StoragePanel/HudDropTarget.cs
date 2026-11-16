using System;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.StoragePanel
{
    /// <summary>
    /// A Main HUD inventory slot registered as a drop/drag peer of open storage panels
    /// (equipment doll, gear strip, hands).
    /// </summary>
    public sealed class HudDropTarget
    {
        public VisualElement Element { get; }
        public AttachedContainer Container { get; }
        public Vector2Int Position { get; }
        public Func<Item> GetItem { get; }

        public HudDropTarget(VisualElement element, AttachedContainer container, Vector2Int position, Func<Item> getItem)
        {
            Element = element;
            Container = container;
            Position = position;
            GetItem = getItem;
        }
    }
}
