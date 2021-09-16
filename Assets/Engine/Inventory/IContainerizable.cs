using UnityEngine;
using System.Collections.Generic;

namespace SS3D.Engine.Inventory
{
    public interface IContainerizable
    {
        Vector2Int Size
        {
            get;
            set;
        }

        float Volume
        {
            get;
        }

        Container Container
        {
            get;
            set;
        }

        Sprite InventorySprite
        {
            get;
        }


        bool InContainer();

        void SetContainer(Container newContainer, bool alreadyAdded, bool alreadyRemoved);

       void SetContainerUnchecked(Container newContainer);

        GameObject GetGameObject();

        List<Trait> Traits 
        {
            get;
            set;
        }

        string ItemId
        {
            get;
        }

        void Freeze();

        void Unfreeze();

        void SetVisibility(bool visible);

        Transform AttachmentPoint
        {
            get;
            set;
        }

        Transform AttachmentPointAlt
        {
            get;
            set;
        }

        void Destroy();

    }
}