using UnityEditor;
using UnityEngine;

namespace SS3D.Content.Creatures.Human
{
    // This adds a button in the editor to test ragdolling
    #if UNITY_EDITOR
    [CustomEditor(typeof(HumanRagdoll))]
    public class RigidBodyInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            HumanRagdoll ragdoll = (HumanRagdoll) target;
            DrawDefaultInspector();
            if (GUILayout.Button(ragdoll.BodyEnabled ? "Disable body" : "Enable body"))
            {
                ragdoll.SetEnabled(!ragdoll.BodyEnabled);
            }
        }
    }
    #endif
}
