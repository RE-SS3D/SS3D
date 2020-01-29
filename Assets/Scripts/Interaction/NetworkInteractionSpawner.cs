using ItemComponents;
using Mirror;
using UnityEngine;

namespace Interaction
{
    /// <summary>
    /// Component used to request the server to spawn various prefabs caused by interactions.
    /// Should contain methods for spawning decals, particle effects and other networked prefabs.
    /// Should be attached to the player prefab.
    /// </summary>
    public class NetworkInteractionSpawner : NetworkBehaviour
    {
        [Command]
        public void CmdSpawnPainterDecal(string painterPropertiesName, Vector3 position, Quaternion rotation)
        {
            PainterProperties properties = Resources.Load<PainterProperties>($"PainterProperties/{painterPropertiesName}");
            if (!properties)
            {
                Debug.LogError($"Could not find PainterProperty {painterPropertiesName} in Resources/PainterProperties");
                return;
            }
            GameObject newObject = Instantiate(properties.DecalPrefab, position, rotation);
            NetworkServer.Spawn(newObject);
        }
    }
}