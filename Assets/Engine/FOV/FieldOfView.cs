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
        private JobHandle finalJobHandle;

        private bool performLateUpdate = false;

        private const float FOG_CEILING = 2.1f;
        private const float FOG_FLOOR = -1.5f;


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
                int[] data = GetTriangleVertexIndices(3*i, numberOfAngles * 3);
                for (int j = 0; j < 6; j++)
                {
                    triangles[12 * i + j] = data[j];
                    triangles[12 * i + j + 6] = data[j] + 1;
                }
            }

            viewCastResults = new NativeArray<ViewCastInfo>(numberOfAngles, Allocator.Persistent);

            angleBuffer = new NativeArray<float>(numberOfAngles, Allocator.Persistent);
            float step = 360f / numberOfAngles;
            for (int i = 0; i < angleBuffer.Length; i++)
            {
                angleBuffer[i] = i * step;
            }
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
                angles = angleBuffer,
                viewCastResults = viewCastResults,
                viewPoints = viewPoints
            };
            JobHandle calculateViewPointsHandle = calculateViewPointsJob.Schedule(fillResultsArrayHandle);

            finalJobHandle = calculateViewPointsHandle;
        }

        private void CompleteMeshCalculationJobs()
        {

            finalJobHandle.Complete();
            int triangleCount = triangles.Length;
            Vector3[] vertices = new Vector3[viewPoints.Length];

            // Surely I can jobify this too.
            for (int i = 0; i < viewPoints.Length; i++)
            {
                vertices[i] = transform.InverseTransformPoint(viewPoints[i]);
            }

            viewMesh.SetVertexBufferParams(vertices.Length, new VertexAttributeDescriptor(VertexAttribute.Position));
            viewMesh.SetVertexBufferData(vertices, 0, 0, vertices.Length);
            viewMesh.SetIndexBufferParams(triangleCount, IndexFormat.UInt32);
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
            [ReadOnly] public NativeArray<float> angles;
            [ReadOnly] public NativeArray<ViewCastInfo> viewCastResults;
            public NativeArray<Vector3> viewPoints;

            public void Execute()
            {

                // Cycle through all the points we returned from our Raycasts.
                for (int i = 0; i < viewCastResults.Length; i++)
                {
                    // For each Raycast hit, we need three vertices.
                    Vector3 point = viewCastResults[i].Point;

                    // This one is the point of our outer circle - well beyond view range.
                    Vector3 outerPoint = origin + DirectionFromAngle(angles[i]) * (fovSettings.viewRange + 10f);
                    viewPoints[3 * i] = new Vector3(outerPoint.x, FOG_CEILING, outerPoint.z);

                    // This one is the one we actually hit - either by hitting a collider, or hitting the max view range.
                    viewPoints[3 * i + 1] = new Vector3(point.x, FOG_CEILING, point.z);

                    // This one is vertically below the one we hit, so that it appears as a volume rather that a plane with gaps.
                    viewPoints[3 * i + 2] = new Vector3(point.x, FOG_FLOOR, point.z);

                }
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

            var rotation = Quaternion.AngleAxis(angleInDegrees, Vector3.up);
            return rotation * Vector3.forward;
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