using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using SS3D.Systems.Entities;

public class HealthController : NetworkBehaviour
{

    public GameObject Ghost;

    [Server]
    private void BecomeGhost(GameObject player, GameObject ghost)
    {
        Entity originEntity = player.GetComponent<Entity>();
        Entity targetEntity = ghost.GetComponent<Entity>();

        Mind originMind = originEntity.Mind;

        targetEntity.SetMind(originMind);
        originEntity.SetMind(Mind.Empty);
    }

    public void ClientKill()
    {
        CmdKill(Ghost, transform);
    }

    [ServerRpc]
    public void CmdKill(GameObject obj, Transform playerTransform)
    {
        GameObject ghost = Instantiate(obj);
        gameObject.transform.Rotate(new Vector3(90, 0, 0));
        ghost.transform.position = playerTransform.position;
        ghost.transform.rotation = playerTransform.rotation;
        ServerManager.Spawn(ghost);
        BecomeGhost(gameObject, ghost);
    }
}

