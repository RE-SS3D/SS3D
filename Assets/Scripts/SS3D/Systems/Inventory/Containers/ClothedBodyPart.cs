using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
	public class ClothedBodyPart : MonoBehaviour
	{
		[SerializeField]
		private ClothType clothType;

		public ClothType Type => clothType;
	}

}