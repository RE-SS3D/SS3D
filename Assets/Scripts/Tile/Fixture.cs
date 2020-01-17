using UnityEngine;
using System.Collections;

namespace TileMap {
    /**
     * Describes an object that is attached to a turf.
     */
    [CreateAssetMenu]
    public class Fixture : ScriptableObject
    {
        public string id;
        public GameObject prefab;
        // Whether this fixture connects to adjacent fixtures
        public bool connects;
    }
}
