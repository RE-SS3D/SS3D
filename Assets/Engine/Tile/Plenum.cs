using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Tiles
{

    /**
     * Describes a plenum beneath a tile that is able to hold wires and pipes
     */
    [CreateAssetMenu]
    public class Plenum : ScriptableObject
    {
        public string id;
        public string genericType;

        public GameObject prefab;
    }
}