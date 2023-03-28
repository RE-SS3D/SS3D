using SS3D.Core;
using SS3D.Core.Settings;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;
using SS3D.Systems;

namespace IngameDebugConsole.Commands
{
	public class ItemCommands
	{
		[ConsoleMethod( "item.addtrait", "Adds a trait to the item in hand" ), UnityEngine.Scripting.Preserve]
		public static void AddTraitToItem( string traitName )
		{
			ItemActor.Item item = GetItemInHand();
			if (item == null)
			{
				Debug.Log("No item in hand");
				return;
			}

            Trait trait = (Trait)ScriptableObject.CreateInstance("Trait");
            trait.Name = traitName;
            item.AddTrait(trait);

			Debug.Log("Trait " + traitName + " added to Item " + item.Name);
        }

		[ConsoleMethod( "item.traits", "Get all traits from item in hand" ), UnityEngine.Scripting.Preserve]
		public static void GetTraitsFromItem()
		{
            ItemActor.Item item = GetItemInHand();
            if (item == null)
            {
                Debug.Log("No item in hand");
                return;
            }

			if (item.Traits.Count == 0)
			{
                Debug.Log("Item in hand has no traits");
                return;
            }

			string debugString = "Item " + item.Name + " has traits: ";
			for (int i = 0; i < item.Traits.Count; i++)
			{
				if (i == item.Traits.Count - 1)
				{
					debugString += item.Traits[i].Name;
				} else
				{
					debugString += item.Traits[i].Name + ", ";
				}
			}

			Debug.Log(debugString);
        }

		private static ItemActor.Item GetItemInHand()
		{
            PlayerSystem playerSystem = Subsystems.Get<PlayerSystem>();
			Soul playerSoul = playerSystem.GetSoul(LocalPlayer.Ckey);
			Entity playerEntity = Subsystems.Get<EntitySystem>().GetSpawnedEntity(playerSoul);
			if (playerEntity == null)
			{
				return null;
			}

			Hands hands = playerEntity.GetComponentInParent<Inventory>().Hands;
			return hands.ItemInHand;
        }
	}
}