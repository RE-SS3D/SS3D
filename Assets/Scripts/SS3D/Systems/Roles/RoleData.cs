using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Roles
{
    [Serializable, CreateAssetMenu(fileName = "Role Data", menuName = "Roles/RoleData")]
    public class RoleData : ScriptableObject
    {
        [SerializeField] private string roleName;
        [SerializeField] private GameObject idCardPrefab;
        [SerializeField] private GameObject pdaPrefab;
        [SerializeField] private List<IDPermission> permissions = new List<IDPermission>();
        [SerializeField] private RoleLoadout loadout;

        public string Name { get => roleName; }
        public GameObject IDCardPrefab { get => idCardPrefab; }
        public GameObject PDAPrefab { get => pdaPrefab; }
        public List<IDPermission> Permissions { get => permissions; }
        public RoleLoadout Loadout { get => loadout; }
    }
}
