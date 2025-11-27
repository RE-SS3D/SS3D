using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// SaveObject that contains all information required to reconstruct a placed item object.
    /// </summary>
    [Serializable]
    public struct SavedPlacedItemObject
    {
        [FormerlySerializedAs("itemName")]
        public string ItemName;

        [FormerlySerializedAs("worldPosition")]
        public Vector3 WorldPosition;

        [FormerlySerializedAs("rotation")]
        public Quaternion Rotation;
    }
}
