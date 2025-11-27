using Coimbra;
using SS3D.Systems.Inventory.Containers;
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
        [field:SerializeField]
        public SerializableDictionary<ContainerType, GameObject> Equipment { get; private set; }

        [field:SerializeField]
        public GameObject HandLeft { get; private set; }

        [field:SerializeField]
        public GameObject HandRight { get; private set; }
    }
}
