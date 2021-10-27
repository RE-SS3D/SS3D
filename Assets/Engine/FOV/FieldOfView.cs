using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Engine.FOV
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class FieldOfView : MonoBehaviour
    {

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

        [NonSerialized]
        public NativeArray<Vector3> viewPoints;

        // Stores triangles for mesh
        private int[] triangles;
        // Buffer for view cast batching
        private NativeArray<ViewCastInfo> viewCastResults;
        // Buffer for view cast angles
        private NativeArray<float> angleBuffer;

        private FovSettings fovSettings;
        private NativeArray<RaycastHit> hits;
        private NativeArray<RaycastCommand> commands;
        private NativeArray<int> viewPointsIndex;
        private JobHandle finalJobHandle;

        private bool performLateUpdate = false;

        public int GetViewPointsIndex()
        {
            return viewPointsIndex[0];
        }

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

            performLateUpdate = true;
        }

        private void LateUpdate()
        {
            if (!performLateUpdate) return;

            performLateUpdate = false;

            // Complete and post-process the jobs
            CompleteMeshCalculationJobs();

            // Dispose of NativeArrays to prevent memory leak
            PerformTemporaryNativeArrayDisposals();
        }

        private void OnDestroy()
        {
            viewMesh.Clear();
            viewPoints.Dispose();
            angleBuffer.Dispose();
            viewCastResults.Dispose();
            viewPointsIndex.Dispose();
        }

        private void OnEnable()
        {
            //fog.SetActive(true);
        }

        private void OnDisable()
        {
            //fog.SetActive(false);
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

            int numberOfAngles = (int)(fovSettings.viewConeWidth * fovSettings.meshResolution);

            int maxViewPoints = numberOfAngles * 3;
            viewPoints = new NativeArray<Vector3>(maxViewPoints, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // Triangles in the mesh will never change - only the positions of the vertices.
            triangles = new int[numberOfAngles * 12];

            // Just trust me on this. It works out in the end.
            for (int i = 0; i < numberOfAngles; i++)
            {
                int[] data = GetTriangleVertexIndices(3*i, numberOfAngles * 12);
                for (int j = 0; j < 6; j++)
                {
                    triangles[i + j] = data[j];
                    triangles[i + j + 6] = data[j] + 1;
                }
            }

            viewCastResults = new NativeArray<ViewCastInfo>(Mathf.RoundToInt(fovSettings.viewConeWidth * fovSettings.meshResolution) + 1, Allocator.Persistent);
            angleBuffer = new NativeArray<float>(viewCastResults.Length, Allocator.Persistent);
            viewPointsIndex = new NativeArray<int>(1, Allocator.Persistent);
        }

        private int[] GetTriangleVertexIndices(int initial, int mod)
        {
            int[] result = new int[6];
            result[0] = initial % mod;
            result[1] = (initial + 3) % mod;
            result[2] = (initial + 1) % mod;
            result[3] = (initial + 1) % mod;
            result[4] = (initial + 3) % mod;
            result[5] = (initial + 4) % mod;
            return result;
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
            prepareForRaycastHandle.Complete();  // For some reason it crashes Unity if we don't complete this job first, despite the dependency being listed below...

            JobHandle performRaycastHandle = RaycastCommand.ScheduleBatch(commands, hits, 128, prepareForRaycastHandle);

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
                    /*
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

                    */
                    viewPoints[index] = viewCastResults[i].Point;///////////////
                    //viewPoints[index] = newViewCast.Point;

                    index++;
                    //oldViewCast = newViewCast;
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
                    commands[i] = new RaycastCommand(origin, DirectionFromAngle(angles[i]), fovSettings.viewRange, fovSettings.obstacleMask, 1);
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
                Vector3 zero = Vector3.zero;
                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit hit = hits[i];

                    if (hit.point != zero)
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

        public static Vector3 DirectionFromAngle(float angleInDegrees, bool angleIsGlobal = true)
        {
            // TODO: Fix the below disaster... It will cause chaos in the editor...

            /*
            if (!angleIsGlobal)
            {
                angleInDegrees += transform.eulerAngles.y;
            }
            */

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