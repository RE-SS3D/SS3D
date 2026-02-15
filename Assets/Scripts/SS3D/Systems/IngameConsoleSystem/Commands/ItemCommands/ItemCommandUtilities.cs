using SS3D.Core.Settings;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Inventory.Items;
using FishNet.Connection;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public static class ItemCommandUtilities
    {
        public static Item GetItemInHand(NetworkConnection conn = null)
        {
            PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();
            Player player = playerSystem.GetPlayer(conn);
            Entity playerEntity = SubSystems.Get<EntitySubSystem>().GetSpawnedEntity(player);
            if (playerEntity == null) return null;
            
            Hands hands = playerEntity.GetComponentInParent<HumanInventory>().Hands;
            return hands.SelectedHand.ItemInHand;
        }
    }
}
