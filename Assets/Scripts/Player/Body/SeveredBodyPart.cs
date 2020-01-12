using UnityEngine;

namespace Player.Body
{
    /// <summary>
    /// MonoBehaviour exists to ensure a severed body part behaves as expected.
    /// Should be attached to any game object that will be spawned as a piece severed from a body
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Item), typeof(Collider))]
    public class SeveredBodyPart : MonoBehaviour
    {
        
    }
}