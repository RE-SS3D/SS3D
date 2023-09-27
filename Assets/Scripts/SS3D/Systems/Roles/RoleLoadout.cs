using Coimbra;
using SS3D.Data.Enums;
using SS3D.Systems.Inventory.Containers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Roles
{
    /// <summary>
    /// The items that should spawn in the player inventory after embarking
    /// </summary>
    [CreateAssetMenu(fileName = "Loadout", menuName = "Roles/Loadout")]
    public class RoleLoadout : ScriptableObject
    {
        public GameObject HandLeft;
        public GameObject HandRight;

        [FormerlySerializedAs("ItemsToEquip")]
        public SerializableDictionary<ContainerType, GameObject> Equipment;
    }
}
