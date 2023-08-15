
using FishNet.Object;
using UnityEngine;


/// <summary>
/// Placeholder class to simulate attacks on other players. Inflicted damage will cause various effects
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

    private void Update()
    {
        CheckForAttack();
    }

    private void CheckForAttack()
    {

        if (!Input.GetKeyDown(KeyCode.F))
        {
            return;
        }

        LayerMask layerMask = LayerMask.GetMask("BodyParts");
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, 10f, layerMask))
        {
            return;
        }

        BodyPart target = hit.collider.GetComponent<BodyPart>();
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
        bodypart.InflictDamageToAllLayer(new DamageTypeQuantity(attackType, damageAmount));
    }

    [ObserversRpc]
    private void RpcInstantiateAttackParticleEffect(Vector3 position)
    {
        Instantiate(attackParticleEffect, position, Quaternion.identity);
    }
}
