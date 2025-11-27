using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Generic scriptableobject that defines common attributes for tiles and items.
    /// </summary>
    public class GenericObjectSo : ScriptableObject
    {
        [FormerlySerializedAs("prefab")]
        [SerializeField]
        private GameObject _prefab;

        [FormerlySerializedAs("icon")]
        [SerializeField]
        private Sprite _icon;

        [NotNull]
        public string NameString => _prefab.name;

        public GameObject Prefab => _prefab;

        public Sprite Icon
        {
            get => _icon;
            set => _icon = value;
        }
    }
}
