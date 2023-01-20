using SS3D.Data.Enums;
using SS3D.Data;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Storage.Containers;
using SS3D.Systems.Storage.Items;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Systems.Storage.Interactions;
using System.Linq;
using System.Security;
using FishNet.Object.Synchronizing;

namespace SS3D.Systems.Items
{
    /// <summary>
    /// What makes you a true Nanotrasen employee
    /// </summary>
    public class PDA : Item, IIdentification
    {
        [SyncVar]
        private Container container;

        public new void Awake()
        {
            base.Awake();

            container = GetComponent<Container>();
        }

        public bool HasPermission(IDPermission _idPermission)
        {
            if (_idPermission == null)
                return true;

            if (container == null)
                return false;

            var _id = container.Items.FirstOrDefault() as IDCard;
            if (_id == null)
                return false;

            return _id.Permissions.Contains(_idPermission);
        }
    }
}