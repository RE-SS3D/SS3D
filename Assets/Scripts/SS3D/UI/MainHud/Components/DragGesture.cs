using UnityEngine;

namespace SS3D.UI.MainHud.Components
{
    /// <summary>
    /// When a press on a HUD slot becomes a drag. Below the threshold the press is still a click: no ghost, no drop
    /// highlights, and the slot's click (take, put, use) runs on release. Same rule as the storage panel's
    /// <c>StorageSlot.DragThresholdPx</c>.
    /// </summary>
    public static class DragGesture
    {
        public const float ThresholdPx = 5f;

        public static bool PastThreshold(Vector2 origin, Vector2 current)
        {
            return (current - origin).sqrMagnitude >= ThresholdPx * ThresholdPx;
        }
    }
}
