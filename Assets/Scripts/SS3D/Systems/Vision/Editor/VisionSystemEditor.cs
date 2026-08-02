using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Vision
{
    [CustomEditor(typeof(VisionSubSystem))]
    public class VisionSystemEditor : Editor
    {
        private VisionSubSystem _vision;

        private SerializedProperty _detectionOffsetProp;
        private SerializedProperty _viewConeWidthProp;
        private SerializedProperty _viewRangeProp;

        protected void OnEnable()
        {
            _vision = (VisionSubSystem)target;

            _detectionOffsetProp = serializedObject.FindProperty("_detectionOffset");
            _viewConeWidthProp = serializedObject.FindProperty("_viewConeWidth");
            _viewRangeProp = serializedObject.FindProperty("_viewRange");
        }

        protected void OnSceneGUI()
        {
            if (!_vision.ShowDebug)
            {
                return;
            }

            serializedObject.Update();

            GUIStyle labelStyle = new()
            {
                fontSize = 14,
            };
            NativeArray<Vector3> viewPoints = _vision.ViewPoints;

            Vector3 viewAngleA = _vision.DirectionFromAngle(-_viewConeWidthProp.floatValue / 2, false);
            Vector3 viewAngleB = _vision.DirectionFromAngle(_viewConeWidthProp.floatValue / 2, false);

            Vector3 center = _vision.Target.position + _detectionOffsetProp.vector3Value;

            Handles.color = Color.green;
            Handles.Label(center, "center");

            labelStyle.normal.textColor = Color.green;
            Handles.DrawWireArc(center, Vector3.up, Vector3.forward, 360, _viewRangeProp.floatValue);

            Handles.DrawLine(center, center + (viewAngleA * _viewRangeProp.floatValue));
            Handles.DrawLine(center, center + (viewAngleB * _viewRangeProp.floatValue));

            // Draw Visual Vision outline;
            Handles.color = Color.yellow;
            GUIStyle style = new();
            style.normal.textColor = Color.red;

            Handles.Label(viewPoints[0], "0", style);
            for (int i = 1; i <= _vision.StepCount; i++)
            {
                Handles.Label(viewPoints[i % _vision.StepCount], i.ToString(), style);
                Handles.color = Color.yellow;
                Handles.DrawLine(viewPoints[i % _vision.StepCount], center);
                Handles.color = Color.cyan;
                Handles.DrawLine(viewPoints[i % _vision.StepCount], viewPoints[(i + 1) % _vision.StepCount]);
            }
        }
    }
}
