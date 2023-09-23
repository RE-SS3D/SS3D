using FishNet.Serializing;
using UnityEngine;

namespace SS3D.Systems.Traits
{
    public static class TraitSerializer
    {
        public static void WriteTrait(this Writer writer, Trait value)
        {
            writer.WriteInt32((int)value.Category);
            writer.WriteString(value.Name);
        }

        /// <summary>
        /// Reads binary data and transforms it in an IDPermission.
        /// </summary>
        public static Trait ReadTrait(this Reader reader)
        {
            TraitCategories category = (TraitCategories)reader.ReadInt32();
            string name = reader.ReadString();

            // Should that really create a new instance or get from an asset database a trait scriptable object.
            Trait trait = ScriptableObject.CreateInstance<Trait>();
            trait.Name = name;
            trait.Category = category;
            return trait;
        }
    }
}
