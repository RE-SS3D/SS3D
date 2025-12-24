using FishNet.Object;
using SS3D.Core;
using System.Linq;
using UnityEngine;
using SS3D.Systems.Health;
using System.Collections;
using UnityEngine.InputSystem;
using InputSubSystem = SS3D.Systems.Inputs.InputSubSystem;

namespace SS3D.Hacks
{
	/// <summary>
	/// Placeholder class to simulate attacks on oneself. Inflicted damage will cause various effects
	/// like bruising and bleeding, but they don't do anything yet. Damage can also sever bodyparts, which is also
	/// only visual at the moment.
	///
	/// Should be attached to player prefab.
	///
	/// Mouse over other players (or yourself) and hit F to attack.
	/// </summary>
	/// 
	public class AttackBodyPartByClickingIt : NetworkBehaviour
	{
		[SerializeField] private GameObject attackParticleEffect;
		[SerializeField] private DamageType attackType;
		[SerializeField][Range(1, 10)] private float damageAmount;
        [SerializeField] private bool _inflictToSingleLayer;
        [SerializeField] private BodyLayerType _bodyLayerType;

        public override void OnStartClient()
		{
			base.OnStartClient();
			if (!IsOwner) enabled = false;
		}

        private void OnEnable()
        {
            InputSubSystem inputSubSystem = SubSystems.Get<InputSubSystem>();

            if (inputSubSystem)
            {
                inputSubSystem.Inputs.Other.Attack.performed += CheckForAttack;
            }
        }

        private void OnDisable()
        {
            InputSubSystem inputSubSystem = SubSystems.Get<InputSubSystem>();

            if (inputSubSystem)
            {
                inputSubSystem.Inputs.Other.Attack.performed -= CheckForAttack;
            }
        }

        private void CheckForAttack(InputAction.CallbackContext callbackContext)
		{
            LayerMask layerMask = LayerMask.GetMask("BodyParts");
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			if (!Physics.Raycast(ray, out hit, 10f, layerMask))
			{
				return;
			}
            BodyPart target = GetComponentsInChildren<BodyPart>().Where(x => x.BodyCollider == hit.collider).FirstOrDefault();
            if (!target)
			{
				return;
			}
            CmdAttackBodyPart(target, damageAmount, hit.point);
		}


		[ServerRpc]
		private void CmdAttackBodyPart(BodyPart bodypart, float damageAmount, Vector3 attackPosition)
		{

			RpcInstantiateAttackParticleEffect(attackPosition);
            if (!_inflictToSingleLayer)
            {
                bodypart.InflictDamageToAllLayer(new DamageTypeQuantity(attackType, damageAmount));
            }
            else
            {
                bodypart.TryInflictDamage(_bodyLayerType, new DamageTypeQuantity(attackType, damageAmount));
            }
        }

        [ObserversRpc]
        private void RpcInstantiateAttackParticleEffect(Vector3 position)
        {
            GameObject attackParticle = Instantiate(attackParticleEffect, position, Quaternion.identity);
            StartCoroutine(DestroyAfterDelay(attackParticle));
        }

        private IEnumerator DestroyAfterDelay(GameObject gameObject)
        {
            yield return new WaitForSeconds(1.0f);
            DestroyImmediate(gameObject);
        }
	}
}
