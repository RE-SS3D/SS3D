using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    [CreateAssetMenu(fileName = "ItemObjectSo", menuName = "TileMap/ItemObjectSo", order = 0)]
    public class ItemObjectSo: ScriptableObject
    {
        [Tooltip("A name for the object. Make sure it is unique.")]
        public string nameString;

        public GameObject prefab;
    }
}