using FishNet.Serializing;
using UnityEngine;

namespace SS3D.Traits
{
    public static class IDPermissionSerializer
    {
        /// <summary>
        /// Writes the IDPermission into binary data to be sent in packets.
        /// </summary>
        public static void WriteIDPermission(this Writer writer, IDPermission value)
        {
            writer.WriteString(value.Name);
            writer.WriteInt32(value.Hash);
        }

        /// <summary>
        /// Reads binary data and transforms it in an IDPermission.
        /// </summary>
        public static IDPermission ReadIDPermission(this Reader reader)
        {
            string name = reader.ReadString();
            int hash = reader.ReadInt32();

            IDPermission permission = ScriptableObject.CreateInstance<IDPermission>();

            permission.Name = name;
            permission.Hash = hash;

            return permission;
        }
    }
}
