using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using Coimbra;
using SS3D.Systems.Health;
using FishNet.Connection;
using SS3D.Core;

public class HealthController : NetworkBehaviour
{

    public GameObject Ghost;

    [Server]
    private void BecomeGhost(GameObject player, GameObject ghost)
    {
        Entity originEntity = player.GetComponent<Entity>();
        Entity ghostEntity = ghost.GetComponent<Entity>();

        Mind originMind = originEntity.Mind;

        ghostEntity.SetMind(originMind);
        destroyObjects(originEntity, ghostEntity);
    }

    [ObserversRpc]
    private void destroyObjects(Entity originEntity, Entity ghostEntity)
    {
        originEntity.gameObject.GetComponent<StaminaController>().Dispose(true);
        originEntity.gameObject.GetComponent<HumanoidController>().Dispose(true);
        originEntity.gameObject.transform.Rotate(new Vector3(90, 0, 0));
        ghostEntity.transform.position = originEntity.transform.position;
        ghostEntity.transform.rotation = originEntity.transform.rotation;
    }

    [Client]
    public void ClientKill()
    {
        CmdKill(Ghost);
    }

    [ServerRpc(RequireOwnership = false)]
    public void CmdKill(GameObject obj)
    {
        GameObject ghost = Instantiate(obj);
        ServerManager.Spawn(ghost);
        BecomeGhost(gameObject, ghost);
    }
}

