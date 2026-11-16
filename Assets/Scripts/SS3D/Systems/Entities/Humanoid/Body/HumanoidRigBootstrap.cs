using SS3D.Systems.Entities.Humanoid.Body;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Auto-binds humanoid rig references at runtime when not configured in the editor.
    /// </summary>
    [RequireComponent(typeof(HumanoidRigReferences))]
    public class HumanoidRigBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            HumanoidRigReferences rig = GetComponent<HumanoidRigReferences>();
            rig.AutoBindBones();
        }
    }
}
