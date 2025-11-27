using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Finger poses are mostly used to indicate how fingers should be placed when holding an item.
    /// </summary>
    public enum FingerPoseType
    {
        Relaxed = 0,
        Gun = 1,
        Flat = 2,
    }
}
