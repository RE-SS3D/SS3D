﻿using Coimbra;

namespace SS3D.Systems.Storage.UI
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