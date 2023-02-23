using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Generic scriptableobject that defines common attributes for tiles and items.
    /// </summary>
    public class GenericObjectSo : ScriptableObject
    {
        [Tooltip("A name for the object. Make sure it is unique.")]
        public string nameString;

        public GameObject prefab;
        public Sprite icon;
    }
}