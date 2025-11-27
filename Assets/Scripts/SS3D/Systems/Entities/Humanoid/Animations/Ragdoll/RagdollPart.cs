using FishNet.Component.Transforming;
using SS3D.Logging;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Component, that identifies GameObjects that are parts of a ragdoll
    /// </summary>
    public class RagdollPart : MonoBehaviour
    {
        protected void OnJointBreak(float breakForce)
        {
            Log.Information(this, "A joint has just been broken!, force: " + breakForce);
        }
    }
}
