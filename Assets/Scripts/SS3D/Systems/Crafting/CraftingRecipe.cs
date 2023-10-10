using Coimbra;
using SS3D.Data.Enums;
using SS3D.Systems.Inventory.Items;
using SS3D.Utils;
using System;
using System.Collections;
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
		private Item _target;

		[SerializeField]
		private List<Item> _result;

		[SerializeField]
		private SerializableDictionary<Item, int> _elements = new();

		/// <summary>
		/// The items and their respective numbers necessary for the recipe.
		/// </summary>
		public Dictionary<Item, int> Elements => _elements;

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
		public Item Target => _target;

		/// <summary>
		/// The result of the crafting.
		/// </summary>
		public List<Item> Result => _result;

		public int ElementsNumber => _elements.Sum(x => x.Value);
	}
}

