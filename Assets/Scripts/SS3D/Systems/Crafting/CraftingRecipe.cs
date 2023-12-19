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
        /// <summary>
        /// The time the crafting should take.
        /// </summary>
		[SerializeField]
		private float _executionTime;

        /// <summary>
        /// The name of the crafting interaction, it's part of recipes as specific interactions are needed for specific crafting, not just recipe ingredients.
        /// </summary>
		[SerializeField]
		private string _interactionName;

        /// <summary>
        /// The target of the crafting, what needs to be clicked on by the player to start the crafting.
        /// </summary>
		[SerializeField]
		private WorldObjectAssetReference _target;

        /// <summary>
        /// A list of resulting objects that will spawn at the end of the crafting process.
        /// </summary>
		[SerializeField]
		private List<WorldObjectAssetReference> _result;

        /// <summary>
        /// Elements of the recipe, that will be consumed in the crafting process.
        /// </summary>
		[SerializeField]
		private SerializableDictionary<WorldObjectAssetReference, int> _elements = new();

        /// <summary>
        /// If true, the target is consumed (despawned).
        /// </summary>
        [SerializeField]
        private bool _consumeTarget;

        /// <summary>
        /// The world objects ids and their respective numbers necessary for the recipe.
        /// </summary>
        public Dictionary<string, int> Elements
		{
			get
			{
				Dictionary<string, int> elements = new Dictionary<string, int>();


				foreach (KeyValuePair<WorldObjectAssetReference,int> keyValuePair in _elements)
				{
					elements.Add(keyValuePair.Key.Id, keyValuePair.Value);
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
		public WorldObjectAssetReference Target => _target;

		/// <summary>
		/// The result of the crafting.
		/// </summary>
		public List<GameObject> Result => _result.Select(reference => reference.Prefab).ToList();

		public int ElementsNumber => _elements.Sum(x => x.Value);

        public bool ConsumeTarget => _consumeTarget;
	}
}

