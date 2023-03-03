using SS3D.Systems.Inventory.Items;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Roles
{
    [CreateAssetMenu(fileName = "Loadout", menuName = "Roles/Loadout")]
    public class RoleLoadout : ScriptableObject
    {
        public GameObject leftHandItem;
        public GameObject rightHandItem;
        public GameObject leftPocketItem;
        public GameObject rightPocketItem;
    }
}
