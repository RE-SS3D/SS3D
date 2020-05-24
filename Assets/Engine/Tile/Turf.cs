using UnityEngine;
using System.Collections;

namespace SS3D.Engine.Tiles
{
    /**
     * Describes a floor or wall on a tile
     */
    [CreateAssetMenu]
    public class Turf : ScriptableObject
    {
        // Should be unique
        public string id;
        // Refers to the general type of object this is, e.g. wall, table, etc.
        public string genericType;

        public bool isWall; // Is otherwise a floor
        public GameObject prefab;

        protected bool Equals(Turf other)
        {
            return base.Equals(other) && id == other.id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Turf) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ (id != null ? id.GetHashCode() : 0);
            }
        }
    }

}
