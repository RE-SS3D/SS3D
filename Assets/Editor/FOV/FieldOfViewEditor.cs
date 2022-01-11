using UnityEditor;
using UnityEngine;

namespace SS3D.Engine.FOV.Editor
{
    [CustomEditor(typeof(FieldOfView))]
    public class FieldOfViewEditor : UnityEditor.Editor
    {
        private FieldOfView fov;

        private SerializedProperty detectionOffsetProp;
        private SerializedProperty viewConeWidthProp;
        private SerializedProperty viewRangeProp;

        private void OnEnable()
        {
            fov = (FieldOfView) target;

            detectionOffsetProp = serializedObject.FindProperty("detectionOffset");
            viewConeWidthProp = serializedObject.FindProperty("viewConeWidth");
            viewRangeProp = serializedObject.FindProperty("viewRange");
        }

        private void OnSceneGUI()
        {
            if (!fov.showDebug) return;
            serializedObject.Update();

            GUIStyle labelStyle = new GUIStyle {fontSize = 14};
            var viewPoints = fov.viewPoints;

            Vector3 viewAngleA = fov.DirectionFromAngle(-viewConeWidthProp.floatValue / 2, false);
            Vector3 viewAngleB = fov.DirectionFromAngle(viewConeWidthProp.floatValue / 2, false);

            var center = fov.target.position + detectionOffsetProp.vector3Value;

            Handles.color = Color.green;
            Handles.Label(center, "center");

            labelStyle.normal.textColor = Color.green;
            Handles.DrawWireArc(center, Vector3.up, Vector3.forward,
                360,
                viewRangeProp.floatValue);

            Handles.DrawLine(center, center + viewAngleA * viewRangeProp.floatValue);
            Handles.DrawLine(center, center + viewAngleB * viewRangeProp.floatValue);

            // Draw Visual FOV outline;
            Handles.color = Color.yellow;
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.red;
           
            Handles.Label(viewPoints[0], "0", style);
            for (int i = 1; i < fov.viewPointsIndex; i++)
            {
                Handles.Label(viewPoints[i], i.ToString(), style);
                Handles.DrawLine(viewPoints[i], center);
            }
        }
    }
}