using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Vision
{
    /// <summary>Scene-view gizmo for the last cast's rays, with <c>Show Debug</c> ticked on the subsystem.</summary>
    [CustomEditor(typeof(VisionSubSystem))]
    public class VisionSystemEditor : UnityEditor.Editor
    {
        private VisionSubSystem _vision;

        protected void OnEnable()
        {
            _vision = (VisionSubSystem)target;
        }

        protected void OnSceneGUI()
        {
            if (!_vision.ShowDebug || _vision.Target == null || !_vision.ViewPoints.IsCreated || _vision.StepCount <= 0)
            {
                return;
            }

            Vector3 center = _vision.DetectionCenter;

            Handles.color = Color.green;
            Handles.Label(center, "ViewPoint");
            Handles.DrawWireArc(center, Vector3.up, Vector3.forward, 360f, VisionSubSystem.ViewRange);

            NativeArray<Vector3> viewPoints = _vision.ViewPoints;
            int count = _vision.StepCount;
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                Handles.color = Color.yellow;
                Handles.DrawLine(center, viewPoints[i]);
                Handles.color = Color.cyan;
                Handles.DrawLine(viewPoints[i], viewPoints[next]);
            }
        }
    }
}
