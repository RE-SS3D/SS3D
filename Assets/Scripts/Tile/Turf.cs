using UnityEngine;
using System.Collections;

namespace TileMap
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
    }

}
