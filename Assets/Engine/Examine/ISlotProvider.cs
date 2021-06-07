using UnityEngine;

namespace SS3D.Engine.Examine
{
    public interface ISlotProvider
    {
		GameObject GetCurrentGameObjectInSlot();
    }
}