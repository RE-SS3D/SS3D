using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror.Examples.Basic
{
	[System.Serializable]
	public class BodyPart
	{
		public string bodyPartName;
		
		//Arrays, so they can be serialized
		public GameObject[] attachedBones; //Any bones that need to have their rigidbodies + colliders replicated for severing
		public Mesh[] attachedMeshes; //Any meshes that need to be replicated and disabled for severing
		public Material mat;
		
		BodyPartBase bpb;
		
		float maxDamage = 100;	//Max damage of any type

		public void Initialize(BodyPartBase bpb) {
			Debug.Log("BODYPART INITIALIZED");
			this.bpb = bpb;
			
			//severMe();
		}

		//To do: Bake some sort of prefab in memory for severing, instead of calculating upon sever
		//You cannot use Mirror attributes on a non-mirror object. 
		//For this reason, a helper on a mirror NetworkBehaviour is called.
		public void severMe() {
			Debug.Log("SEVER ME");
			bpb.severHelper(this);
		}
	}
}