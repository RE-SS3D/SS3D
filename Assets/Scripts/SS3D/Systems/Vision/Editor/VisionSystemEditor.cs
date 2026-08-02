@@ -1,10 +1,11 @@
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Vision
{
    [CustomEditor(typeof(VisionSubSystem))]
    public class VisionSystemEditor : UnityEditor.Editor
    public class VisionSystemEditor : Editor
    {
        private VisionSubSystem _vision;

@@ -16,30 +17,30 @@ namespace SS3D.Systems.Vision
        {
            _vision = (VisionSubSystem)target;

            _detectionOffsetProp = serializedObject.FindProperty("detectionOffset");
            _viewConeWidthProp = serializedObject.FindProperty("viewConeWidth");
            _viewRangeProp = serializedObject.FindProperty("viewRange");
            _detectionOffsetProp = serializedObject.FindProperty("_detectionOffset");
            _viewConeWidthProp = serializedObject.FindProperty("_viewConeWidth");
            _viewRangeProp = serializedObject.FindProperty("_viewRange");
        }

        protected void OnSceneGUI()
        {
            if (!_vision.showDebug)
            if (!_vision.ShowDebug)
            {
                return;
            }

            serializedObject.Update();

            GUIStyle labelStyle = new GUIStyle
            GUIStyle labelStyle = new()
            {
                fontSize = 14,
            };
            Unity.Collections.NativeArray<Vector3> viewPoints = _vision.viewPoints;
            NativeArray<Vector3> viewPoints = _vision.ViewPoints;

            Vector3 viewAngleA = _vision.DirectionFromAngle(-_viewConeWidthProp.floatValue / 2, false);
            Vector3 viewAngleB = _vision.DirectionFromAngle(_viewConeWidthProp.floatValue / 2, false);

            Vector3 center = _vision.target.position + _detectionOffsetProp.vector3Value;
            Vector3 center = _vision.Target.position + _detectionOffsetProp.vector3Value;

            Handles.color = Color.green;
            Handles.Label(center, "center");
@@ -52,17 +53,17 @@ namespace SS3D.Systems.Vision

            // Draw Visual Vision outline;
            Handles.color = Color.yellow;
            GUIStyle style = new GUIStyle();
            GUIStyle style = new();
            style.normal.textColor = Color.red;

            Handles.Label(viewPoints[0], "0", style);
            for (int i = 1; i <= _vision.stepCount; i++)
            for (int i = 1; i <= _vision.StepCount; i++)
            {
                Handles.Label(viewPoints[i % _vision.stepCount], i.ToString(), style);
                Handles.Label(viewPoints[i % _vision.StepCount], i.ToString(), style);
                Handles.color = Color.yellow;
                Handles.DrawLine(viewPoints[i % _vision.stepCount], center);
                Handles.DrawLine(viewPoints[i % _vision.StepCount], center);
                Handles.color = Color.cyan;
                Handles.DrawLine(viewPoints[i % _vision.stepCount], viewPoints[(i + 1) % _vision.stepCount]);
                Handles.DrawLine(viewPoints[i % _vision.StepCount], viewPoints[(i + 1) % _vision.StepCount]);
            }
        }
    }
