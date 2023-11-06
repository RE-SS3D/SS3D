using Coimbra;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Inventory.Items;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
	/// <summary>
	/// Crafting recipes allow to replace a bunch of item by another, using a specific interaction.
	/// </summary>
	[CreateAssetMenu(fileName = "Recipe", menuName = "SS3D/Crafting/Recipe")]
	public class CraftingRecipe : ScriptableObject
	{
		[SerializeField]
		private float _executionTime;

		[SerializeField]
		private string _interactionName;

		[SerializeField]
		private WorldObjectAssetReference _target;

		[SerializeField]
		private List<WorldObjectAssetReference> _result;

		[SerializeField]
		private SerializableDictionary<WorldObjectAssetReference, int> _elements = new();

		/// <summary>
		/// The items and their respective numbers necessary for the recipe.
		/// </summary>
		public Dictionary<Item, int> Elements
		{
			get
			{
				Dictionary<Item, int> elements = new Dictionary<Item, int>();

				foreach (KeyValuePair<WorldObjectAssetReference,int> keyValuePair in _elements)
				{
					elements.Add(keyValuePair.Key.Prefab.GetComponent<Item>(), keyValuePair.Value);
				}

				return elements;
			}
		}

		/// <summary>
		/// Time it takes in second for the crafting to finish.
		/// </summary>
		public float ExecutionTime => _executionTime;

		/// <summary>
		/// Name of the necessary interaction to perform the recipe.
		/// </summary>
		public string InteractionName => _interactionName;

		/// <summary>
		/// The target of the recipe, which is the item on which the player must click to get the crafting interactions.
		/// </summary>
		public Item Target => _target.Prefab.GetComponent<Item>();

		/// <summary>
		/// The result of the crafting.
		/// </summary>
		public List<GameObject> Result => _result.Select(reference => reference.Prefab).ToList();

		public int ElementsNumber => _elements.Sum(x => x.Value);
	}
}

