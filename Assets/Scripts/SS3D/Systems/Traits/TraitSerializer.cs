using FishNet.Serializing;
using SS3D.Logging;
using SS3D.Systems.Inventory.Items;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems
{
    public class TraitSerializer
    {
        public static void WriteTrait(this Writer writer, Trait value)
        {
            writer.WriteInt32((int) value.Category);
            writer.WriteArray(value.Name.ToCharArray());
        }

        /// <summary>
        /// Reads binary data and transforms it in an IDPermission.
        /// </summary>
        public static Trait ReadTrait(this Reader reader)
        {
            var actor = (ItemActor)reader.ReadNetworkBehaviour();
            return actor.GetItem;
        }
    }

}
