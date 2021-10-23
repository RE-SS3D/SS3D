using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Engine.FOV
{
    public class AlternateFieldOfView : MonoBehaviour
    {
        [SerializeField]
        public bool showDebug;

        [SerializeField]
        private GameObject fog;

        [SerializeField]
        public Transform target = null;

        [Space]
        [SerializeField]
        private float viewRange = 5;

        [Range(0, 360)]
        [SerializeField]
        [Tooltip("The field of view width")]
        private float viewConeWidth = 360;

        [SerializeField]
        [Tooltip("Which layers this can't see through")]
        private LayerMask obstacleMask = 0;

        [SerializeField]
        [Tooltip("Raycasts per degree")]
        private float meshResolution = .1f;

        [Header("Edge Detection")]
        [SerializeField]
        [Range(0, 10)]
        [Tooltip("How many checks are done to make edges appear close to corners")]
        private int edgeResolveIterations = 0;

        [SerializeField]
        [Range(0, 10)]
        [Tooltip("How far a corner has to be to be checked behind another corner")]
        private float edgeDistanceThreshold = 0;

        [SerializeField]
        [Tooltip("The center of the field of view's actual wall detection")]
        private Vector3 detectionOffset = Vector3.zero;

        private MeshFilter viewMeshFilter;
        private Mesh viewMesh;

        const int MAX_VERTICES = 800;

        [NonSerialized]
        public NativeArray<Vector3> viewPoints;

        // Stores triangles for mesh
        private NativeArray<ushort> triangles;
        // Buffer for view cast batching
        private NativeArray<ViewCastInfo> viewCastResults;
        // Buffer for view cast angles
        private NativeArray<float> angleBuffer;

        private FovSettings fovSettings;
        private NativeArray<RaycastHit> hits;
        private NativeArray<RaycastCommand> commands;
        private NativeArray<int> viewPointsIndex;
        private JobHandle finalJobHandle;

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            // Field of View should not update before player spawn.
            if (!target) return;

            // Ensure the FOV follows the player
            transform.position = target.transform.position;

            // Allocate any NativeArrays required for our jobs
            PerformTemporaryNativeArrayAllocations();

            // Schedule the jobs (to be completed in LateUpdate)
            ScheduleMeshCalculationJobs();
        }

        private void LateUpdate()
        {
            if (!target) return;

            // Complete and post-process the jobs
            CompleteMeshCalculationJobs();

            // Dispose of NativeArrays to prevent memory leak
            PerformTemporaryNativeArrayDisposals();
        }

        private void OnDestroy()
        {
            viewMesh.Clear();
            viewPoints.Dispose();
            triangles.Dispose();
            angleBuffer.Dispose();
            viewCastResults.Dispose();
            viewPointsIndex.Dispose();
        }

        private void OnEnable()
        {
            fog.SetActive(true);
        }

        private void OnDisable()
        {
            fog.SetActive(false);
        }

        private void PerformTemporaryNativeArrayAllocations()
        {
            hits = new NativeArray<RaycastHit>(angleBuffer.Length, Allocator.TempJob);
            commands = new NativeArray<RaycastCommand>(angleBuffer.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        }

        private void PerformTemporaryNativeArrayDisposals()
        {
            hits.Dispose();
            commands.Dispose();
        }

        private void Initialize()
        {
            // Only run when graphics are present
            // TODO: Only run on client
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Destroy(gameObject);
                return;
            }

            // Wrap up the FOV settings so we can pass to the Job System conveniently
            fovSettings = new FovSettings(meshResolution, viewConeWidth, viewRange, edgeDistanceThreshold, edgeResolveIterations, obstacleMask);

            viewMeshFilter = GetComponent<MeshFilter>();

            viewMesh = new Mesh();
            viewMesh.name = "View Mesh";
            viewMeshFilter.mesh = viewMesh;
            
            int maxViewPoints = (int)(fovSettings.viewConeWidth * fovSettings.meshResolution) * 3 + 1;
            viewPoints = new NativeArray<Vector3>(maxViewPoints + 1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            triangles = new NativeArray<ushort>((maxViewPoints + 1) * 3, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (ushort i = 0; i < maxViewPoints - 2; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = unchecked((ushort)(i + 1u));
                triangles[i * 3 + 2] = unchecked((ushort)(i + 2u));
            }

            viewCastResults = new NativeArray<ViewCastInfo>(Mathf.RoundToInt(fovSettings.viewConeWidth * fovSettings.meshResolution) + 1, Allocator.Persistent);
            angleBuffer = new NativeArray<float>(viewCastResults.Length, Allocator.Persistent);
            viewPointsIndex = new NativeArray<int>(1, Allocator.Persistent);
        }


        private void ScheduleMeshCalculationJobs()
        { 
            PrepareForRaycastJob prepareForRaycastJob = new PrepareForRaycastJob
            {
                fovSettings = fovSettings,
                origin = target.transform.position,
                angles = angleBuffer,
                commands = commands
            };
            JobHandle prepareForRaycastHandle = prepareForRaycastJob.Schedule();

            JobHandle performRaycastHandle = RaycastCommand.ScheduleBatch(commands, hits, 32, prepareForRaycastHandle);

            FillResultsArrayJob fillResultsArrayJob = new FillResultsArrayJob
            {
                fovSettings = fovSettings,
                origin = target.transform.position,
                hits = hits,
                angles = angleBuffer,
                viewCastResults = viewCastResults
            };
            JobHandle fillResultsArrayHandle = fillResultsArrayJob.Schedule(performRaycastHandle);

            CalculateViewPointsJob calculateViewPointsJob = new CalculateViewPointsJob
            {
                fovSettings = fovSettings,
                origin = target.transform.position,
                viewCastResults = viewCastResults,
                viewPointsIndex = viewPointsIndex,
                viewPoints = viewPoints
            };
            JobHandle calculateViewPointsHandle = calculateViewPointsJob.Schedule(fillResultsArrayHandle);

            finalJobHandle = calculateViewPointsHandle;
        }

        private void CompleteMeshCalculationJobs()
        {
            
            finalJobHandle.Complete();
            int vpIndex = viewPointsIndex[0];
            int triangleCount = (vpIndex - 2) * 3;
            viewPoints[0] = transform.InverseTransformPoint(target.transform.position);
            Vector3[] vertices = new Vector3[vpIndex];
            for (int i = 1; i < vpIndex; i++) vertices[i] = transform.InverseTransformPoint(viewPoints[i]);
            viewMesh.SetVertexBufferParams(vpIndex, new VertexAttributeDescriptor(VertexAttribute.Position));
            viewMesh.SetVertexBufferData(vertices, 0, 0, vpIndex);
            viewMesh.SetIndexBufferParams(triangleCount, IndexFormat.UInt16);
            viewMesh.SetIndexBufferData(triangles, 0, 0, triangleCount);
            viewMesh.subMeshCount = 1;
            viewMesh.SetSubMesh(0, new SubMeshDescriptor(0, triangleCount));
        }

        /// <summary>
        /// This stores the FOV settings in a single struct, allowing for convenient passing to the Job System.
        /// </summary>
        public struct FovSettings
        {
            public float meshResolution;
            public float viewConeWidth;
            public float viewRange;
            public float edgeDistanceThreshold;
            public int edgeResolveIterations;
            public LayerMask obstacleMask;

            public FovSettings(float meshResolution, float viewConeWidth, float viewRange, float edgeDistanceThreshold, int edgeResolveIterations, LayerMask obstacleMask)
            {
                this.meshResolution = meshResolution;
                this.viewConeWidth = viewConeWidth;
                this.viewRange = viewRange;
                this.edgeDistanceThreshold = edgeDistanceThreshold;
                this.edgeResolveIterations = edgeResolveIterations;
                this.obstacleMask = obstacleMask;
            }
        }

        #region Job Definitions

        private struct CalculateViewPointsJob : IJob
        {
            public FovSettings fovSettings;
            public Vector3 origin;
            [ReadOnly] public NativeArray<ViewCastInfo> viewCastResults;
            public NativeArray<int> viewPointsIndex;
            public NativeArray<Vector3> viewPoints;

            public void Execute()
            {
                int index = 1;
                ViewCastInfo oldViewCast = new ViewCastInfo();
                int stepCount = Mathf.RoundToInt(fovSettings.viewConeWidth * fovSettings.meshResolution);
                for (int i = 0; i < stepCount; i++)
                {
                    ViewCastInfo newViewCast = viewCastResults[i];
                    bool edgeDistanceThresholdExceeded =
                        Mathf.Abs(oldViewCast.Distance - newViewCast.Distance) > fovSettings.edgeDistanceThreshold;
                    if (oldViewCast.Hit != newViewCast.Hit ||
                        (oldViewCast.Hit && newViewCast.Hit && oldViewCast.Normal != newViewCast.Normal &&
                         edgeDistanceThresholdExceeded))
                    {
                        EdgeInfo edge = FindEdge(oldViewCast, newViewCast, fovSettings, origin);
                        if (edge.PointA != Vector3.zero)
                        {
                            viewPoints[index] = edge.PointA;
                            index++;
                        }
                        if (edge.PointB != Vector3.zero)
                        {
                            viewPoints[index] = edge.PointB;
                            index++;
                        }
                    }

                    viewPoints[index] = newViewCast.Point;
                    index++;
                    oldViewCast = newViewCast;
                }

                viewPointsIndex[0] = index;
            }
        }

        private struct PrepareForRaycastJob : IJob
        {
            public FovSettings fovSettings;
            public Vector3 origin;
            public NativeArray<float> angles;
            public NativeArray<RaycastCommand> commands;


            public void Execute()
            {
                int stepCount = Mathf.RoundToInt(fovSettings.viewConeWidth * fovSettings.meshResolution);
                float stepAngleSize = fovSettings.viewConeWidth / stepCount;
                float halfCone = fovSettings.viewConeWidth / 2;

                // Set the angles. TODO: only set this in Start(), as it doesn't change.
                for (int i = 0; i <= stepCount; i++)
                {
                    angles[i] = halfCone + stepAngleSize * i;
                }

                // Create Raycast commands
                for (int i = 0; i < angles.Length; i++)
                {
                    commands[i] = new RaycastCommand(origin, DirectionFromAngle(angles[i]), fovSettings.viewRange, fovSettings.obstacleMask);
                }
            }
        }

        private struct FillResultsArrayJob : IJob
        {
            public FovSettings fovSettings;
            public Vector3 origin;
            [ReadOnly] public NativeArray<RaycastHit> hits;
            [ReadOnly] public NativeArray<float> angles;
            public NativeArray<ViewCastInfo> viewCastResults;

            public void Execute()
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit hit = hits[i];

                    if (hit.collider)
                    {
                        // There was a hit! Use the hit point.
                        viewCastResults[i] = new ViewCastInfo(true, hit.point, hit.distance, angles[i], hit.normal);
                    }
                    else
                    {
                        // We didn't hit anything. Calculate the point at the required angle and distance.
                        viewCastResults[i] = new ViewCastInfo(false,
                            origin + DirectionFromAngle(angles[i]) * fovSettings.viewRange,
                            fovSettings.viewRange,
                            angles[i],
                            hit.normal);
                    }
                }
            }

        }


        #endregion

        public static Vector3 DirectionFromAngle(float angleInDegrees)
        {
            var rotation = Quaternion.AngleAxis(angleInDegrees, Vector3.up);
            return rotation * Vector3.forward;
        }

        private static EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast, FovSettings fovSettings, Vector3 origin)
        {
            float minAngle = minViewCast.Angle;
            float maxAngle = maxViewCast.Angle;
            Vector3 minPoint = Vector3.zero;
            Vector3 maxPoint = Vector3.zero;

            for (int i = 0; i < fovSettings.edgeResolveIterations; i++)
            {
                float angle = (minAngle + maxAngle) / 2;
                ViewCastInfo newViewCast = ViewCast(angle, fovSettings, origin);

                bool edgeDistanceThresholdExceeded =
                    Mathf.Abs(minViewCast.Distance - newViewCast.Distance) > fovSettings.edgeDistanceThreshold;
                if (newViewCast.Hit == minViewCast.Hit && !edgeDistanceThresholdExceeded)
                {
                    minAngle = angle;
                    minPoint = newViewCast.Point;
                }
                else
                {
                    maxAngle = angle;
                    maxPoint = newViewCast.Point;
                }
            }

            return new EdgeInfo(minPoint, maxPoint);
        }

        private static ViewCastInfo ViewCast(float globalAngle, FovSettings fovSettings, Vector3 origin)
        {
            Vector3 dir = DirectionFromAngle(globalAngle);

            if (Physics.Raycast(origin, dir, out var hit, fovSettings.viewRange, fovSettings.obstacleMask))
            {
                return new ViewCastInfo(true, hit.point, hit.distance,
                    globalAngle,
                    hit.normal);
            }

            return new ViewCastInfo(false, origin + dir * fovSettings.viewRange, fovSettings.viewRange,
                globalAngle, hit.normal);
        }

    }



    public struct EdgeInfo
    {
        public Vector3 PointA;
        public Vector3 PointB;

        public EdgeInfo(Vector3 pointA, Vector3 pointB)
        {
            PointA = pointA;
            PointB = pointB;
        }
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