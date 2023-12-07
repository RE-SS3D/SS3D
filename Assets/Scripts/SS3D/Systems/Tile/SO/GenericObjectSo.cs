using JetBrains.Annotations;
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
        [NotNull]
        public string NameString => prefab.name;

        public GameObject prefab;
        public Sprite icon;
    }
}