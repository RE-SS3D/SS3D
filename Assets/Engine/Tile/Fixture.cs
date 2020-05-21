using UnityEngine;
using System.Collections;

namespace SS3D.Engine.Tiles {

    /**
     * Describes the different layers on which fixtures can be placed.
     */
    public enum FixtureLayers
    {
        Furniture,
        Electrical,
        Disposal,
        Pipes_1,
        Pipes_2,
        Pipes_3,
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
