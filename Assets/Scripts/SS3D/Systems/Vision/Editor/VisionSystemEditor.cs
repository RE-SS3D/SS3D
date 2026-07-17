using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Vision
{
    [CustomEditor(typeof(VisionSubSystem))]
    public class VisionSystemEditor : UnityEditor.Editor
    {
        private VisionSubSystem _vision;

        private SerializedProperty _viewConeWidthProp;
        private SerializedProperty _viewRangeProp;

        protected void OnEnable()
        {
            _vision = (VisionSubSystem)target;

            _viewConeWidthProp = serializedObject.FindProperty("viewConeWidth");
            _viewRangeProp = serializedObject.FindProperty("viewRange");
        }

        protected void OnSceneGUI()
        {
            if (!_vision.showDebug || _vision.target == null)
            {
                return;
            }

            if (!_vision.viewPoints.IsCreated || _vision.stepCount <= 0)
            {
                return;
            }

            serializedObject.Update();

            float range = _viewRangeProp.floatValue;
            float halfCone = _viewConeWidthProp.floatValue * 0.5f;
            Vector3 center = _vision.DetectionCenter;
            Vector3 edgeA = _vision.DirectionFromAngle(-halfCone, false);
            Vector3 edgeB = _vision.DirectionFromAngle(halfCone, false);

            Handles.color = Color.green;
            Handles.Label(center, "ViewPoint");
            Handles.DrawWireArc(center, Vector3.up, Vector3.forward, 360f, range);
            Handles.DrawLine(center, center + (edgeA * range));
            Handles.DrawLine(center, center + (edgeB * range));

            Handles.color = Color.yellow;
            GUIStyle labelStyle = new GUIStyle { normal = { textColor = Color.red } };
            Unity.Collections.NativeArray<Vector3> viewPoints = _vision.viewPoints;
            int count = _vision.stepCount;

            Handles.Label(viewPoints[0], "0", labelStyle);
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                Handles.Label(viewPoints[i], i.ToString(), labelStyle);
                Handles.color = Color.yellow;
                Handles.DrawLine(center, viewPoints[i]);
                Handles.color = Color.cyan;
                Handles.DrawLine(viewPoints[i], viewPoints[next]);
            }
        }
    }
}
