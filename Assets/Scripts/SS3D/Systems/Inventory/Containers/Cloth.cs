using SS3D.Systems.Inventory.Containers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
	public class Cloth : MonoBehaviour
	{
		[SerializeField]
		private ClothType clothType;

		public ClothType Type => clothType;
	}
}
