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
        public static bool TryGetLocalPlayerEntity(out Entity entity)
        {
            PlayerSystem playerSystem = Subsystems.Get<PlayerSystem>();
            Soul playerSoul = playerSystem.GetSoul(LocalPlayer.Ckey);
            entity = Subsystems.Get<EntitySystem>().GetSpawnedEntity(playerSoul);
            if (entity != null) return true;
            return false;
        }
        public static Item GetItemInHand()
        {
            if(!TryGetLocalPlayerEntity(out Entity playerEntity))
            { 
                return null;
            }
            Hands hands = playerEntity.GetComponentInParent<Inventory>().Hands;
            return hands.ItemInHand;
        }
    }
}
