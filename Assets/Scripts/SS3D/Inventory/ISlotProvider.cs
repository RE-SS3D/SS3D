using UnityEngine;

namespace SS3D.Inventory
{
    public interface ISlotProvider
    {
		GameObject GetCurrentGameObjectInSlot();
    }
}