using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Vision.Editor
{
    [CustomEditor(typeof(VisionSystem))]
    public class VisionEditor : UnityEditor.Editor
    {
        private VisionSystem vision;

        private SerializedProperty detectionOffsetProp;
        private SerializedProperty viewConeWidthProp;
        private SerializedProperty viewRangeProp;

        private void OnEnable()
        {
            vision = (VisionSystem) target;

            detectionOffsetProp = serializedObject.FindProperty("detectionOffset");
            viewConeWidthProp = serializedObject.FindProperty("viewConeWidth");
            viewRangeProp = serializedObject.FindProperty("viewRange");
        }

        private void OnSceneGUI()
        {
            if (!vision.showDebug) return;
            serializedObject.Update();

            GUIStyle labelStyle = new GUIStyle {fontSize = 14};
            var viewPoints = vision.viewPoints;

            Vector3 viewAngleA = vision.DirectionFromAngle(-viewConeWidthProp.floatValue / 2, false);
            Vector3 viewAngleB = vision.DirectionFromAngle(viewConeWidthProp.floatValue / 2, false);

            var center = vision.target.position + detectionOffsetProp.vector3Value;

            Handles.color = Color.green;
            Handles.Label(center, "center");

            labelStyle.normal.textColor = Color.green;
            Handles.DrawWireArc(center, Vector3.up, Vector3.forward,
                360,
                viewRangeProp.floatValue);

            Handles.DrawLine(center, center + viewAngleA * viewRangeProp.floatValue);
            Handles.DrawLine(center, center + viewAngleB * viewRangeProp.floatValue);

            // Draw Visual Vision outline;
            Handles.color = Color.yellow;
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.red;
           
            Handles.Label(viewPoints[0], "0", style);
            for (int i = 1; i <= vision.stepCount; i++)
            {
                Handles.Label(viewPoints[i % vision.stepCount], i.ToString(), style);
                                Handles.color = Color.yellow;
                Handles.DrawLine(viewPoints[i % vision.stepCount], center);
                Handles.color = Color.cyan;
                Handles.DrawLine(viewPoints[i % vision.stepCount], viewPoints[(i + 1) % vision.stepCount]);
            }
        }
    }
}