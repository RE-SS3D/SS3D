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
        public string id;
        public bool isWall; // Is otherwise a floor
        public GameObject prefab;
    }

}
