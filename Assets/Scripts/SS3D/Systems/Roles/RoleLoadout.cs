using Coimbra;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Roles
{
    /// <summary>
    /// The items that should spawn in the player inventory after embarking
    /// </summary>
    [CreateAssetMenu(fileName = "Loadout", menuName = "Roles/Loadout")]
    public class RoleLoadout : ScriptableObject
    {
        public ObjectAssetReference HandLeftAsset;
        public ObjectAssetReference HandRightAsset;

        public SerializableDictionary<ContainerType, ObjectAssetReference> EquipmentAssets;
    }
}
