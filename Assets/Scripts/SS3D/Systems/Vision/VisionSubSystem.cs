using System;
using FishNet;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using SS3D.Systems.Entities.Events;
using Coimbra;
using Coimbra.Services.Events;

namespace SS3D.Systems.Vision
{
    public class VisionSubSystem : Core.Behaviours.SubSystem
    {
        [SerializeField]
        public bool showDebug;
        [SerializeField]
        private Texture2D visionMap;

        [SerializeField]
        public Transform target = null;

        [Space]
        [SerializeField]
        private float viewRange = 35;

        [Range(0, 360)]
        [SerializeField]
        [Tooltip("The field of view width")]
        private float viewConeWidth = 360;

        [SerializeField]
        [Tooltip("Which layers this can't see through")]
        private LayerMask obstacleMask = 0;

        [SerializeField]
        [Tooltip("Raycasts per degree")]
        private float resolution = 1f;

        [SerializeField]
        [Tooltip("The center of the field of view's actual wall detection")]
        private Vector3 detectionOffset = Vector3.zero;
        
        [NonSerialized]
        public NativeArray<Vector3> viewPoints;
        [NonSerialized]
        public int stepCount;
        
        // Buffer for view cast batching
        private ViewCastInfo[] viewCastResults;
        // Buffer for view cast angles
        private float[] angleBuffer;
        
        static ProfilerMarker MapPerformanceMarker = new ProfilerMarker("Vision.VisionMap");
        static ProfilerMarker PointsPerformanceMarker = new ProfilerMarker("Vision.ViewPoints");

        private bool _clientVisionInitialized;

        private Vector3 DetectionCenter => target.transform.position + detectionOffset;

        protected override void OnAwake()
        {
            base.OnAwake();

            AddHandle(LocalPlayerObjectChanged.AddListener(HandlePlayerObjectChanged));
        }

        protected override void OnEnabled()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                gameObject.Dispose(true);
                return;
            }

            if (InstanceFinder.IsServerOnly)
            {
                return;
            }

            _clientVisionInitialized = true;

            visionMap = new Texture2D(Mathf.RoundToInt(viewConeWidth * resolution), 1, TextureFormat.R16, false);
            visionMap.wrapMode = TextureWrapMode.Repeat;
            visionMap.filterMode = FilterMode.Bilinear;
            Shader.SetGlobalTexture("_VisionMap", visionMap);

            stepCount = Mathf.CeilToInt(viewConeWidth * resolution);
            viewPoints = new NativeArray<Vector3>(stepCount + 1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            
            viewCastResults = new ViewCastInfo[stepCount + 1];
            angleBuffer = new float[viewCastResults.Length];
        }

        protected override void OnDisabled()
        {
            if (!_clientVisionInitialized)
            {
                return;
            }

            viewPoints.Dispose();
            _clientVisionInitialized = false;
        }

        private void HandlePlayerObjectChanged(ref EventContext context, in LocalPlayerObjectChanged e)
        {
            target = e.PlayerObject.transform;
        }

        private void LateUpdate()
        {
            if (!_clientVisionInitialized || !target)
            {
                return;
            }

            transform.position = target.transform.position;
            float angle = target.transform.rotation.eulerAngles.y * Mathf.Deg2Rad;

            Shader.SetGlobalVector("_PlayerPos", DetectionCenter);
            Shader.SetGlobalFloat("_PlayerAngle", angle);
            Shader.SetGlobalFloat("_ViewConeWidth", viewConeWidth * Mathf.Deg2Rad);
            Shader.SetGlobalFloat("_ViewRange", viewRange);
            
            DrawVisionMap();
        }

        public Vector3 DirectionFromAngle(float angleInDegrees, bool angleIsGlobal)
        {
            if (!angleIsGlobal)
            {
                angleInDegrees += target.transform.eulerAngles.y;
            }
            
            Quaternion rotation = Quaternion.AngleAxis(angleInDegrees, Vector3.up);
            return rotation * Vector3.forward;
        }

        private void DrawVisionMap()
        {
            PointsPerformanceMarker.Begin();
            CalculateViewPoints();
            PointsPerformanceMarker.End();

            MapPerformanceMarker.Begin();

            if (visionMap.width != stepCount)
            {
                visionMap.Reinitialize(stepCount, 1);
            }
 
            Color[] depths = new Color[stepCount + 1];
            for (int i = 0; i < stepCount; i++)
            {
                Vector3 positionOS = viewPoints[i % stepCount] - DetectionCenter;
                positionOS.y = 0;
                depths[i] = new Color(positionOS.magnitude / viewRange, 0, 0);
            }

#pragma warning disable UNT0017 // SetPixels invocation is slow
            visionMap.SetPixels(depths);
#pragma warning restore UNT0017 // SetPixels invocation is slow
            visionMap.Apply();
            
            MapPerformanceMarker.End();
        }

        private void CalculateViewPoints()
        {
            stepCount = Mathf.CeilToInt(viewConeWidth * resolution);
            float stepAngleSize = viewConeWidth / stepCount;
            float halfCone = viewConeWidth / 2;

            if (viewCastResults.Length < stepCount)
            {
                Array.Resize(ref viewCastResults, stepCount + 1);
                Array.Resize(ref angleBuffer, stepCount + 1);
                viewPoints.Dispose();
                viewPoints = new NativeArray<Vector3>(stepCount + 1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            for (int i = 0; i < stepCount; i++)
            {
                angleBuffer[i] = (target.transform.rotation.eulerAngles.y - halfCone) + (stepAngleSize * i);
            }

            ViewCastBatch(angleBuffer, viewCastResults);
            for (int i = 0; i < stepCount; i++)
            {
                ViewCastInfo newViewCast = viewCastResults[i];

                viewPoints[i] = viewCastResults[i].Point;
            }
        }

        private void ViewCastBatch(float[] angles, ViewCastInfo[] resultArray)
        {
            if (resultArray.Length < angles.Length)
            {
                throw new ArgumentException("Results can't be smaller than angles", nameof(resultArray));
            }

            // Allocate arrays for raycast data
            NativeArray<RaycastHit> hits = new NativeArray<RaycastHit>(angles.Length, Allocator.TempJob);
            NativeArray<RaycastCommand> commands = new NativeArray<RaycastCommand>(angles.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            
            Vector3 origin = target.transform.position + detectionOffset;

            // Create raycast commands
            for (int i = 0; i < angles.Length; i++)
            {
                commands[i] = new RaycastCommand(origin, DirectionFromAngle(angles[i], true), viewRange, obstacleMask);
            }

            // Schedule raycasts
            JobHandle handle = RaycastCommand.ScheduleBatch(commands, hits, 1);

            // Wait for the raycasting to complete
            handle.Complete();
            
            // Fill results array
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];

                // Collider is only valid if hit (yes, this is in the docs)
                if (hit.collider)
                {
                    resultArray[i] = new ViewCastInfo(true, hit.point, hit.distance, angles[i], hit.normal);
                }
                else
                {
                    resultArray[i] = new ViewCastInfo(
                        false, 
                        origin + (DirectionFromAngle(angles[i], true) * viewRange),
                        viewRange,
                        angles[i],
                        hit.normal);
                }
            }
            
            // Dispose raycast data
            hits.Dispose();
            commands.Dispose();
        }
        
        public struct ViewCastInfo
        {
            public bool Hit;
            public Vector3 Point;
            public float Distance;
            public float Angle;
            public Vector3 Normal;

            public ViewCastInfo(bool hit, Vector3 point, float distance, float angle, Vector3 normal)
            {
                Hit = hit;
                Point = point;
                Distance = distance;
                Angle = angle;
                Normal = normal;
            }
        }
    }
}