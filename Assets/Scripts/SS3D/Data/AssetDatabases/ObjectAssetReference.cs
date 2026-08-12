using SS3D.Attributes;
using System;
using UnityEngine;

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// This SO is used to reference world object assets in inspector fields without worrying about losing data.
    /// World object assets are anything that can be placed in the world as a GameObject, like items and tileobjects.
    /// </summary>
    public sealed partial class ObjectAssetReference : ScriptableObject
    {
#if UNITY_EDITOR
        [ReadOnly]
#endif
        [Header("This file is auto-generated, do not modify it manually")]
        public string Id;


        public override bool Equals(object other) => other is ObjectAssetReference otherReference && Id.Equals(otherReference.Id);

        public override int GetHashCode() => HashCode.Combine(Id);

        private bool Equals([JetBrains.Annotations.NotNull] ObjectAssetReference other) => Id == other.Id;
    }
}