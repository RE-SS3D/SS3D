using FishNet.Serializing;
using SS3D.Logging;
using SS3D.Systems;
using SS3D.Systems.Inventory.Items;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items
{
    using Item = ItemActor.Item;
    public static class ItemSerializer 
    {
   
    /// <summary>
    /// Writes the IDPermission into binary data to be sent in packets.
    /// </summary>
    public static void WriteItem(this Writer writer, Item value)
    {
            if(value.Actor == null)
            {
                Punpun.Error(typeof(ItemSerializer), "can't serialize item with null Actor reference");
            }
            writer.WriteNetworkBehaviour(value.Actor);
    }

    /// <summary>
    /// Reads binary data and transforms it in an IDPermission.
    /// </summary>
    public static Item ReadItem(this Reader reader)
    {
        var actor = (ItemActor) reader.ReadNetworkBehaviour();
        return actor.GetItem;
    }
}
} 
