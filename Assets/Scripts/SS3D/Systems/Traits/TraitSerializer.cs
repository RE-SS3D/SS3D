using FishNet.Serializing;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Logging;
using SS3D.Systems.Inventory.Items;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems
{
    public static class TraitSerializer
    {
        public static void WriteTrait(this Writer writer, Trait value)
        {
            writer.WriteInt32((int) value.Category);
            writer.WriteString(value.Name);
        }

        /// <summary>
        /// Reads binary data and transforms it in an IDPermission.
        /// </summary>
        public static Trait ReadTrait(this Reader reader)
        {
            var category = (TraitCategories) reader.ReadInt32();
            var name = reader.ReadString();
            // Should that really create a new instance or get from an asset database a trait scriptable object.
            var trait = ScriptableObject.CreateInstance<Trait>();
            trait.Name = name;
            trait.Category = category;
            return trait;
        }
    }

}
