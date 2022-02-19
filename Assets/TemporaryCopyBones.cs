using UnityEngine;

namespace SS3D
{
    public class TemporaryCopyBones : MonoBehaviour
    {
        public SkinnedMeshRenderer OriginalMesh;
        public SkinnedMeshRenderer TargetMesh;

        private void Start()
        {
            TargetMesh.bones = OriginalMesh.bones;
        }
    }
}
