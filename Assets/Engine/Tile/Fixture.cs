using UnityEngine;
using System.Collections;

namespace Engine.Tiles {
    /**
     * Describes an object that is attached to a turf.
     */
    [CreateAssetMenu]
    public class Fixture : ScriptableObject
    {
        public string id;
        public string genericType;

        public GameObject prefab;
    }
}
