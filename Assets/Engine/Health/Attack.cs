using Mirror;
using UnityEngine;
using SS3D.Content.Systems.Player.Body; // TODO: This is bad.

namespace SS3D.Engine.Health
{
    /// <summary>
    /// Placeholder class to simulate attacks on other players. Inflicted damage will cause various effects
    /// like bruising and bleeding, but they don't do anything yet. Damage can also sever bodyparts, which is also
    /// only visual at the moment.
    ///
    /// Should be attached to player prefab.
    ///
    /// Mouse over other players (or yourself) and hit F to attack.
    /// </summary>
    public class Attack : NetworkBehaviour
    {
        [SerializeField] private GameObject attackParticleEffect = null;
        [SerializeField] private AttackType attackType = AttackType.Blunt;
        [SerializeField][Range(1,10)] private float damageAmount = 1f;

        private void Update()
        {
            CheckForAttack();
        }

        private void CheckForAttack()
        {
            if (!isLocalPlayer)
            {
                return;
            }
            
            if (!Input.GetKeyDown(KeyCode.F))
            {
                return;
            }

            LayerMask layerMask = ~(1 << LayerMask.NameToLayer ("Player"));
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if(!Physics.Raycast(ray, out hit, 10f, layerMask))
            {
                return;
            }

            BodyPart target = hit.collider.GetComponent<BodyPart>();
            if (!target)
            {
                return;
            }

            CmdAttackBodyPart(target.Body.gameObject, target.BodyPartType, damageAmount, hit.point);
        }

        //TODO: should depend on the circumstances of the attack
        private DamageType DecideDamageType(AttackType attackType, float damageAmount)
        {
            return DamageType.Brute;
        }

        [Command]
        private void CmdAttackBodyPart(GameObject bodyGameObject, BodyPartType bodyPartType, float damageAmount, Vector3 attackPosition)
        {
            DamageType damageType = DecideDamageType(attackType, damageAmount);
            Body body = bodyGameObject.GetComponent<Body>();
            if (body == null)
            {
                return;
            }

            RpcInstantiateAttackParticleEffect(attackPosition);
            float bodyPartStatusChangeRoll = Random.Range(0, 100);
            body.RpcDoDamageToBodyPart(bodyPartType, damageType, damageAmount, bodyPartStatusChangeRoll);
        }

        [ClientRpc]
        private void RpcInstantiateAttackParticleEffect(Vector3 position)
        {
            Instantiate(attackParticleEffect, position, Quaternion.identity);
        }
    }
}
