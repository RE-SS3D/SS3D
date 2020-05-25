using UnityEditor;
using UnityEngine;

namespace SS3D.Content.Creatures.Human
{
    #if UNITY_EDITOR
    [CustomEditor(typeof(HumanRigidBody))]
    public class RigidBodyInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            HumanRigidBody rigidBody = (HumanRigidBody) target;
            DrawDefaultInspector();
            if (GUILayout.Button(rigidBody.BodyEnabled ? "Disable body" : "Enable body"))
            {
                rigidBody.BodyEnabled = !rigidBody.BodyEnabled;
            }
        }
    }
    #endif
}
