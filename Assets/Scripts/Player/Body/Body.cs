using System.Collections.Generic;
using System.Linq;
using Enums;
using Mirror;
using UnityEngine;

namespace Player.Body
{
	/// <summary>
	/// Class deals with receiving damage to the player body and tracking the status of each individual BodyPart.
	/// Should be attached to the player prefab.
	/// </summary>
	public class Body : NetworkBehaviour
	{
		///The BodyPart objects that this MonoBehaviour is responsible for managing
		private List<BodyPart> bodyParts = new List<BodyPart>();
        private void Start()
        {
            //update the bodypart list
            bodyParts.Clear();

            foreach (BodyPart part in gameObject.GetComponentsInChildren<BodyPart>())
            {
                bodyParts.Add(part);
            }
        }

        //TODO: implement bleeding
        //TODO: handle missing body parts appropriately. Missing hand should prevent using it. Missing foot should slow you down or make it impossible to stand, etc.
        [ClientRpc]
		public void RpcDoDamageToBodyPart(BodyPartType bodyPartType, DamageType damageType, float damageAmount, float serverAuthoritativeRandomRoll)
		{
			BodyPart bodyPart = FindBodyPart(bodyPartType);
			if (bodyPart == null)
			{
				return;
			}
			
			Debug.Log($"Damaging {bodyPart.gameObject.name} for {damageAmount} as {damageType}.");
			float totalDamage = bodyPart.BodyPartDamages.First(bodyPartDamage => bodyPartDamage.DamageType == damageType).Damage(damageAmount);

			switch (damageType)
			{
				case DamageType.Brute:
					bodyPart.EvaluateStatusChange(BodyPartStatuses.Numb, totalDamage, serverAuthoritativeRandomRoll);
					bodyPart.EvaluateStatusChange(BodyPartStatuses.Bruised, totalDamage, serverAuthoritativeRandomRoll);
					bodyPart.EvaluateStatusChange(BodyPartStatuses.Bleeding, totalDamage, serverAuthoritativeRandomRoll);
					bodyPart.EvaluateStatusChange(BodyPartStatuses.Crippled, totalDamage, serverAuthoritativeRandomRoll);
					bodyPart.EvaluateStatusChange(BodyPartStatuses.Severed, totalDamage, serverAuthoritativeRandomRoll);
					break;
				case DamageType.Burn:
					bodyPart.EvaluateStatusChange(BodyPartStatuses.Burned, totalDamage, serverAuthoritativeRandomRoll);
					bodyPart.EvaluateStatusChange(BodyPartStatuses.Blistered, totalDamage, serverAuthoritativeRandomRoll);
					break;
				case DamageType.Toxic:
					bodyPart.EvaluateStatusChange(BodyPartStatuses.Numb, totalDamage, serverAuthoritativeRandomRoll);
					bodyPart.EvaluateStatusChange(BodyPartStatuses.Blistered, totalDamage, serverAuthoritativeRandomRoll);
					break;
				case DamageType.Suffocation:
					bodyPart.EvaluateStatusChange(BodyPartStatuses.Numb, totalDamage, serverAuthoritativeRandomRoll);
					break;
				default:
					Debug.LogError($"Body does not have damage type handling for {damageType}!");
					break;
			}
		}

		public void SeverBodyPart(BodyPart bodyPart)
		{
			if (!isServer)
			{
				return;
			}
			CmdSeverBodyPart(bodyPart.BodyPartType);
		}

		//TODO: Severing an arm should not sever the hand separately. The severed children pieces should be attached to each other.
		[Command]
		public void CmdSeverBodyPart(BodyPartType bodyPartType)
		{
			BodyPart bodyPart = FindBodyPart(bodyPartType);
			if (bodyPart == null)
			{
				return;
			}
			
			GameObject mainBodypart = Instantiate(bodyPart.SeveredBodyPartPrefab, transform.position, Quaternion.identity);
            UpdateBodyPartVisuals(mainBodypart.GetComponent<SkinnedMeshRenderer>(), bodyPart.SkinnedMeshRenderer); 

            NetworkServer.Spawn(mainBodypart);

			bodyPart.ChildrenParts.ForEach(child =>
			{
                GameObject childBodyPart = Instantiate(child.SeveredBodyPartPrefab, child.transform.position, Quaternion.identity);
                UpdateBodyPartVisuals(childBodyPart.GetComponent<SkinnedMeshRenderer>(), child.SkinnedMeshRenderer);

                NetworkServer.Spawn(childBodyPart);
            });
			RpcHideSeveredBodyPart(bodyPartType);
		}

		[ClientRpc]
		private void RpcHideSeveredBodyPart(BodyPartType bodyPartType)
		{
			BodyPart bodyPart = FindBodyPart(bodyPartType);
			if (bodyPart == null)
			{
				return;
			}
			
			bodyPart.SkinnedMeshRenderer.enabled = false;
			bodyPart.ChildrenParts.ForEach(childBodyPart => childBodyPart.SkinnedMeshRenderer.enabled = false);
		}

		private BodyPart FindBodyPart(BodyPartType bodyPartType)
		{
			return bodyParts.FirstOrDefault(bodyParts => bodyParts.BodyPartType == bodyPartType);
		}

        private void UpdateBodyPartVisuals(SkinnedMeshRenderer newBodyPart, SkinnedMeshRenderer bodyPart)
        {
            Material[] materials = bodyPart.sharedMaterials;
            newBodyPart.materials = materials;

            for (int i = 0; i < newBodyPart.sharedMesh.blendShapeCount; i++)
            {
                newBodyPart.SetBlendShapeWeight(i, bodyPart.GetBlendShapeWeight(i));
            }
        }
    }
}