using SS3D.Core.Settings;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Inventory.Items;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    using Inventory = Inventory.Containers.Inventory;
    public static class ItemCommandUtilities
    {
        public static Item GetItemInHand()
        {
            PlayerSubSystem playerSystem = Subsystems.Get<PlayerSubSystem>();
            Player player = playerSystem.GetPlayer(LocalPlayer.Ckey);
            Entity playerEntity = Subsystems.Get<EntitySubSystem>().GetSpawnedEntity(player);
            if (playerEntity == null)
            {
                return null;
            }

            Hands hands = playerEntity.GetComponentInParent<Inventory>().Hands;
            return hands.ItemInHand;
        }
    }
}
