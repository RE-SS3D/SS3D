using System;
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
        public bool ShowDebug => _showDebug;

        [SerializeField]
        public Transform Target = null;
        
        [NonSerialized]
        public NativeArray<Vector3> ViewPoints;

        [NonSerialized]
        public int StepCount;
        
        [SerializeField]
        private Texture2D _visionMap;
        
        [Space]
        [SerializeField]
        private float _viewRange = 35;
        
        [SerializeField]
        private bool _showDebug;

        [Range(0, 360)]
        [SerializeField]
        [Tooltip("The field of view width")]
        private float _viewConeWidth = 360;

        [SerializeField]
        [Tooltip("Which layers this can't see through")]
        private LayerMask _obstacleMask = 0;

        [SerializeField]
        [Tooltip("Raycasts per degree")]
        private float _resolution = 1f;

        // TODO: actually change the center of the field
        [SerializeField]
        [Tooltip("The center of the field of scene debug view. TODO: The center of the field of view's actual wall detection")]
        private Vector3 _detectionOffset = Vector3.zero;
        
        /// <summary>
        /// Buffer for view cast batching
        /// </summary>
        private ViewCastInfo[] _viewCastResults;
        
        /// <summary>
        /// Buffer for view cast angles
        /// </summary>
        private float[] _angleBuffer;

        private static ProfilerMarker MapPerformanceMarker = new ProfilerMarker("Vision.VisionMap");
        private static ProfilerMarker PointsPerformanceMarker = new ProfilerMarker("Vision.ViewPoints");

        protected override void OnAwake()
        {
            base.OnAwake();

            AddHandle(LocalPlayerObjectChanged.AddListener(HandlePlayerObjectChanged));
        }

        protected override void OnEnabled()
        {
            // Only run when graphics are present
            // TODO: Only run on client
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                GameObject.Dispose(gameObject);

                return;
            }
            
            _visionMap = new Texture2D(Mathf.RoundToInt(_viewConeWidth * _resolution), 1, TextureFormat.R16, false);
            _visionMap.wrapMode = TextureWrapMode.Repeat;
            _visionMap.filterMode = FilterMode.Bilinear;
            Shader.SetGlobalTexture("_VisionMap", _visionMap);

            StepCount = Mathf.CeilToInt(_viewConeWidth * _resolution);
            ViewPoints = new NativeArray<Vector3>(StepCount + 1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            
            _viewCastResults = new ViewCastInfo[StepCount + 1];
            _angleBuffer = new float[_viewCastResults.Length];
        }

        protected override void OnDisabled()
        {
            ViewPoints.Dispose();
        }

        private void HandlePlayerObjectChanged(ref EventContext context, in LocalPlayerObjectChanged e)
        {
            Target = e.PlayerObject.transform;
        }

        private void LateUpdate()
        {
            if (!Target) 
            {
                return; 
            }

            transform.position = Target.transform.position;
            float angle = Target.transform.rotation.eulerAngles.y * Mathf.Deg2Rad;

            Shader.SetGlobalVector("_PlayerPos", Target.transform.position);
            Shader.SetGlobalFloat("_PlayerAngle", angle);
            Shader.SetGlobalFloat("_ViewConeWidth", _viewConeWidth * Mathf.Deg2Rad);
            Shader.SetGlobalFloat("_ViewRange", _viewRange);
            
            DrawVisionMap();
        }

        public Vector3 DirectionFromAngle(float angleInDegrees, bool angleIsGlobal)
        {
            if (!angleIsGlobal)
            {
                angleInDegrees += Target.transform.eulerAngles.y;
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

            if (_visionMap.width != StepCount)
            {
                _visionMap.Reinitialize(StepCount, 1);
            }
 
            Color[] depths = new Color[StepCount];
            for (int i = 0; i < StepCount; i++)
            {
                Vector3 positionOS = ViewPoints[i % StepCount] - Target.transform.position;
                positionOS.y = 0;
                depths[i] = new Color(positionOS.magnitude / _viewRange, 0, 0);
            }

#pragma warning disable UNT0017 // SetPixels invocation is slow
            _visionMap.SetPixels(depths);
#pragma warning restore UNT0017 // SetPixels invocation is slow
            _visionMap.Apply();
            
            MapPerformanceMarker.End();
        }

        private void CalculateViewPoints()
        {
            StepCount = Mathf.CeilToInt(_viewConeWidth * _resolution);
            float stepAngleSize = _viewConeWidth / StepCount;
            float halfCone = _viewConeWidth / 2;

            if (_viewCastResults.Length < StepCount)
            {
                Array.Resize(ref _viewCastResults, StepCount + 1);
                Array.Resize(ref _angleBuffer, StepCount + 1);
                ViewPoints.Dispose();
                ViewPoints = new NativeArray<Vector3>(StepCount + 1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            for (int i = 0; i < StepCount; i++)
            {
                _angleBuffer[i] = (Target.transform.rotation.eulerAngles.y - halfCone) + (stepAngleSize * i);
            }

            ViewCastBatch(_angleBuffer, _viewCastResults);
            for (int i = 0; i < StepCount; i++)
            {
                ViewCastInfo newViewCast = _viewCastResults[i];

                ViewPoints[i] = _viewCastResults[i].Point;
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
            
            Vector3 origin = Target.transform.position;

            // Create raycast commands
            for (int i = 0; i < angles.Length; i++)
            {
                commands[i] = new RaycastCommand(origin, DirectionFromAngle(angles[i], true), _viewRange, _obstacleMask);
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
                        origin + (DirectionFromAngle(angles[i], true) * _viewRange),
                        _viewRange,
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
