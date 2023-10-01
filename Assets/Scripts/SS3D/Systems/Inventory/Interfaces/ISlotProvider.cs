using UnityEngine;

namespace SS3D.Systems.Inventory.Interfaces
{
    public interface ISlotProvider
    {
        GameObject GetCurrentGameObjectInSlot();
    }
}