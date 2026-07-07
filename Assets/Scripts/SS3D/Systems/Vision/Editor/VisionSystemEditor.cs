using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Vision
{
    [CustomEditor(typeof(VisionSubSystem))]
    public class VisionSystemEditor : UnityEditor.Editor
    {
        private VisionSubSystem _vision;

        private SerializedProperty _detectionOffsetProp;
        private SerializedProperty _viewConeWidthProp;
        private SerializedProperty _viewRangeProp;

        protected void OnEnable()
        {
            _vision = (VisionSubSystem)target;

            _detectionOffsetProp = serializedObject.FindProperty("detectionOffset");
            _viewConeWidthProp = serializedObject.FindProperty("viewConeWidth");
            _viewRangeProp = serializedObject.FindProperty("viewRange");
        }

        protected void OnSceneGUI()
        {
            if (!_vision.showDebug || _vision.target == null)
            {
                return;
            }

            serializedObject.Update();

            float range = _viewRangeProp.floatValue;
            float halfCone = _viewConeWidthProp.floatValue / 2f;
            float yaw = _vision.target.eulerAngles.y;

            Vector3 center = _vision.target.position + _detectionOffsetProp.vector3Value;
            Vector3 edgeA = DirectionFromAngle(yaw - halfCone);
            Vector3 edgeB = DirectionFromAngle(yaw + halfCone);

            Handles.color = Color.green;
            Handles.Label(center, "center");
            Handles.DrawWireArc(center, Vector3.up, Vector3.forward, 360, range);

            Handles.color = Color.yellow;
            Handles.DrawLine(center, center + (edgeA * range));
            Handles.DrawLine(center, center + (edgeB * range));
        }

        private static Vector3 DirectionFromAngle(float angleDegrees)
        {
            return Quaternion.AngleAxis(angleDegrees, Vector3.up) * Vector3.forward;
        }
    }
}
