using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Vision
{
    [CustomEditor(typeof(VisionSystem))]
    public class VisionSystemEditor : UnityEditor.Editor
    {
        private VisionSystem _vision;

        private SerializedProperty _detectionOffsetProp;
        private SerializedProperty _viewConeWidthProp;
        private SerializedProperty _viewRangeProp;

        protected void OnEnable()
        {
            _vision = (VisionSystem)target;

            _detectionOffsetProp = serializedObject.FindProperty("detectionOffset");
            _viewConeWidthProp = serializedObject.FindProperty("viewConeWidth");
            _viewRangeProp = serializedObject.FindProperty("viewRange");
        }

        protected void OnSceneGUI()
        {
            if (!_vision.showDebug)
            {
                return;
            }

            serializedObject.Update();

            GUIStyle labelStyle = new GUIStyle
            {
                fontSize = 14,
            };
            Unity.Collections.NativeArray<Vector3> viewPoints = _vision.viewPoints;

            Vector3 viewAngleA = _vision.DirectionFromAngle(-_viewConeWidthProp.floatValue / 2, false);
            Vector3 viewAngleB = _vision.DirectionFromAngle(_viewConeWidthProp.floatValue / 2, false);

            Vector3 center = _vision.target.position + _detectionOffsetProp.vector3Value;

            Handles.color = Color.green;
            Handles.Label(center, "center");

            labelStyle.normal.textColor = Color.green;
            Handles.DrawWireArc(center, Vector3.up, Vector3.forward, 360, _viewRangeProp.floatValue);

            Handles.DrawLine(center, center + (viewAngleA * _viewRangeProp.floatValue));
            Handles.DrawLine(center, center + (viewAngleB * _viewRangeProp.floatValue));

            // Draw Visual Vision outline;
            Handles.color = Color.yellow;
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.red;

            Handles.Label(viewPoints[0], "0", style);
            for (int i = 1; i <= _vision.stepCount; i++)
            {
                Handles.Label(viewPoints[i % _vision.stepCount], i.ToString(), style);
                Handles.color = Color.yellow;
                Handles.DrawLine(viewPoints[i % _vision.stepCount], center);
                Handles.color = Color.cyan;
                Handles.DrawLine(viewPoints[i % _vision.stepCount], viewPoints[(i + 1) % _vision.stepCount]);
            }
        }
    }
}
