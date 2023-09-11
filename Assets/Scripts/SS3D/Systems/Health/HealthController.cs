using UnityEngine;
using FishNet.Object;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using Coimbra;
using SS3D.Systems.Health;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;

public class HealthController : NetworkBehaviour
{
    public GameObject Ghost;
    private GameObject _spawnedGhost;

    [Server]
    private void BecomeGhost(GameObject player, GameObject ghost)
    {
        Entity originEntity = player.GetComponent<Entity>();
        Entity ghostEntity = ghost.GetComponent<Entity>();

        Mind originMind = originEntity.Mind;

        ghostEntity.SetMind(originMind);
        RpcDestroyObjects(originEntity);
        RpcUpdateGhostPosition(originEntity, ghostEntity);
    }

    [ObserversRpc]
    private void RpcDestroyObjects(Entity originEntity)
    {
        GameObject originEntityGameObject = originEntity.gameObject;
        // TODO: Optimize these GetComponents, this is a temporary solution.
        originEntityGameObject.GetComponent<Hands>().Dispose(true);
        originEntityGameObject.GetComponent<HumanInventory>().Dispose(true);
        originEntityGameObject.GetComponent<InteractionController>().Dispose(true);
        originEntityGameObject.GetComponent<StaminaController>().Dispose(true);
        originEntityGameObject.GetComponent<HumanoidController>().Dispose(true);
        
    }

    [ObserversRpc]
    private void RpcUpdateGhostPosition(Entity originEntity, Entity ghostEntity)
    {
        ghostEntity.Transform.SetPositionAndRotation(originEntity.Transform.position, originEntity.Transform.rotation);
        originEntity.Transform.Rotate(new Vector3(90, 0, 0));
    }



    [Server]
    public void Kill()
    {
        _spawnedGhost = Instantiate(Ghost);
        ServerManager.Spawn(_spawnedGhost);
        BecomeGhost(gameObject, _spawnedGhost);
    }
}

