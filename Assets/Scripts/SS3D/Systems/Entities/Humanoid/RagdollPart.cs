using FishNet.Component.Transforming;
using UnityEngine;

/// <summary>
/// Component, that identifies GameObjects that are parts of a ragdoll
/// </summary>
[RequireComponent(typeof(Collider), typeof(Rigidbody), typeof(NetworkTransform))]
public class RagdollPart : MonoBehaviour
{
    
}
