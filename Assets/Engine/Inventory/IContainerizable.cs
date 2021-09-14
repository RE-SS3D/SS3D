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


        public bool InContainer();

        public void SetContainer(Container newContainer, bool alreadyAdded, bool alreadyRemoved);

        public void SetContainerUnchecked(Container newContainer);

        public GameObject GetGameObject();

        public List<Trait> Traits 
        {
            get;
            set;
        }


        public void Destroy();

    }
}