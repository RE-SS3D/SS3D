using SS3D.Systems.Entities.Humanoid.Body;
using UnityEditor;
using UnityEngine;

namespace SS3D.Editor
{
    /// <summary>
    /// Editor utility to auto-bind humanoid rig bone references on prefabs.
    /// </summary>
    public static class HumanoidRigSetupUtility
    {
        [MenuItem("SS3D/Animation/Auto-Bind Humanoid Rig References")]
        public static void AutoBindSelectedRig()
        {
            foreach (GameObject obj in Selection.gameObjects)
            {
                HumanoidRigReferences rig = obj.GetComponent<HumanoidRigReferences>();
                if (rig == null)
                {
                    rig = obj.AddComponent<HumanoidRigReferences>();
                }
                rig.AutoBindBones();
                EditorUtility.SetDirty(rig);
            }
        }
    }
}
