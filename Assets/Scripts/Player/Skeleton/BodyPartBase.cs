using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror.Examples.Basic
{
	public class BodyPartBase : NetworkBehaviour
	{
		//For surgery, and maybe hit detection
		public BodyPart[] bodyParts = new BodyPart[] {};
		public Mesh myMeshTest;
		public Material myMaterialTest;
		public float mass = 0f;
		public GameObject severedBodyPart;
		
		public void Awake() {
			Initialize();
		}
		
		void Initialize() {
			for (int i = 0; i < bodyParts.Length; i++) {
				bodyParts[i].Initialize(this);
			}
		}
		
		//See BodyPart.severMe()
		[Server]
		public void severHelper(BodyPart bp) {
			Debug.Log("Severing limb");
			GameObject newSeveredBodyPart = Object.Instantiate(severedBodyPart) as GameObject;
			newSeveredBodyPart.AddComponent<YeahImAwake>();	//To test if instantiation works
			MeshFilter mf = newSeveredBodyPart.GetComponent<MeshFilter>();
			MeshCollider mc = newSeveredBodyPart.GetComponent<MeshCollider>();
			MeshRenderer mr = newSeveredBodyPart.GetComponent<MeshRenderer>();
			
			//Get meshes
			mf.mesh = myMeshTest;	//Using a placeholder mesh for testing
			mc.sharedMesh = myMeshTest;
			mc.convex = true;
			
			//Get rigidbodies
			Rigidbody rb = newSeveredBodyPart.GetComponent<Rigidbody>();
			rb.mass = mass;	//Using a placeholder mass for testing, _might_ change later if im not too lazy :^y
			
			//Get materials
			mr.material = myMaterialTest;
			
			//Create new gameobject and throw everything in
			Object.Instantiate(newSeveredBodyPart);
		}
	}
}