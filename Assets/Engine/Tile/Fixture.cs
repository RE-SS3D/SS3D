using UnityEngine;
using System.Collections;

namespace SS3D.Engine.Tiles {

    public enum FixtureLayers
    {
        Furniture,
        Electrical,
        Disposal,
        Pipes_1,
        Pipes_2,
        Pipes_3,
        // NumberOfLayers, // Small trick to allocate the array to the correct size
    }

    /**
     * Describes an object that is attached to a turf.
     */
    [CreateAssetMenu]
    public class Fixture : ScriptableObject
    {
        public string id;
        public string genericType;

        public FixtureLayers layer;
        public GameObject prefab;
    }
}
