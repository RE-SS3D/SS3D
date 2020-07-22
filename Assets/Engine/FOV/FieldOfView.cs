using System;
using System.Collections.Generic;
using Mirror;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Engine.FOV
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class FieldOfView : MonoBehaviour
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
        [NonSerialized]
        public int viewPointsIndex;
        
        // Stores triangles for mesh
        private NativeArray<ushort> triangles;
        // Buffer for view cast batching
        private ViewCastInfo[] viewCastResults;
        // Buffer for view cast angles
        private float[] angleBuffer;
        
        static ProfilerMarker meshPerformanceMarker = new ProfilerMarker("FieldOfView.Mesh");
        static ProfilerMarker pointsPerformanceMarker = new ProfilerMarker("FieldOfView.ViewPoints");

        private void OnEnable()
        {
            fog.SetActive(true);
        }

        private void OnDisable()
        {
            fog.SetActive(false);
        }

        private void Start()
        {
            // Only run when graphics are present
            // TODO: Only run on client
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Destroy(gameObject);
            }
            
            viewMeshFilter = GetComponent<MeshFilter>();

            viewMesh = new Mesh();
            viewMesh.name = "View Mesh";
            viewMeshFilter.mesh = viewMesh;

            int maxViewPoints = (int) (viewConeWidth * meshResolution) * 3 + 1;
            viewPoints = new NativeArray<Vector3>(maxViewPoints + 1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            triangles = new NativeArray<ushort>((maxViewPoints + 1) * 3, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (ushort i = 0; i < maxViewPoints - 2; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = unchecked((ushort) (i + 1u));
                triangles[i * 3 + 2] = unchecked((ushort) (i + 2u));
            }
            
            viewCastResults = new ViewCastInfo[Mathf.RoundToInt(viewConeWidth * meshResolution) + 1];
            angleBuffer = new float[viewCastResults.Length];
        }

        private void OnDestroy()
        {
            viewMesh.Clear();
            viewPoints.Dispose();
            triangles.Dispose();
        }

        private void Update()
        {
            if(!target) return;
            transform.position = target.transform.position;
            DrawFieldOfView();
        }

        public Vector3 DirectionFromAngle(float angleInDegrees, bool angleIsGlobal)
        {
            if (!angleIsGlobal)
            {
                angleInDegrees += transform.eulerAngles.y;
            }
            
            var rotation = Quaternion.AngleAxis(angleInDegrees, Vector3.up);
            return rotation * Vector3.forward;
        }

        public void DrawFieldOfView()
        {
            pointsPerformanceMarker.Begin();
            CalculateViewPoints();
            pointsPerformanceMarker.End();

            meshPerformanceMarker.Begin();

            int triangleCount = (viewPointsIndex - 2) * 3;
            viewPoints[0] = transform.InverseTransformPoint(target.transform.position);
            Vector3[] vertices = new Vector3[viewPointsIndex];
            for(int i = 1; i < viewPointsIndex; i++) vertices[i] = transform.InverseTransformPoint(viewPoints[i]);
            viewMesh.SetVertexBufferParams(viewPointsIndex, new VertexAttributeDescriptor(VertexAttribute.Position));
            viewMesh.SetVertexBufferData(vertices, 0, 0, viewPointsIndex);
            viewMesh.SetIndexBufferParams(triangleCount, IndexFormat.UInt16);
            viewMesh.SetIndexBufferData(triangles, 0, 0, triangleCount);
            viewMesh.subMeshCount = 1;
            viewMesh.SetSubMesh(0, new SubMeshDescriptor(0, triangleCount));
            
            meshPerformanceMarker.End();
        }

        public void CalculateViewPoints()
        {
            int stepCount = Mathf.RoundToInt(viewConeWidth * meshResolution);
            float stepAngleSize = viewConeWidth / stepCount;
            float halfCone = viewConeWidth / 2;

            // Resize when changed in editor
            if (viewCastResults.Length < stepCount)
            {
                Array.Resize(ref viewCastResults, stepCount + 1);
                Array.Resize(ref angleBuffer, stepCount + 1);
            }

            // Set required angles
            for (var i = 0; i <= stepCount; i++)
            {
                angleBuffer[i] = halfCone + stepAngleSize * i;
            }
            
            // Perform raycast batch
            ViewCastBatch(angleBuffer, viewCastResults);
            
            viewPointsIndex = 1;
            ViewCastInfo oldViewCast = new ViewCastInfo();
            for (int i = 0; i <= stepCount; i++)
            {
                ViewCastInfo newViewCast = viewCastResults[i];

                bool edgeDistanceThresholdExceeded =
                    Mathf.Abs(oldViewCast.Distance - newViewCast.Distance) > edgeDistanceThreshold;
                if (oldViewCast.Hit != newViewCast.Hit ||
                    (oldViewCast.Hit && newViewCast.Hit && oldViewCast.Normal != newViewCast.Normal &&
                     edgeDistanceThresholdExceeded))
                {
                    EdgeInfo edge = FindEdge(oldViewCast, newViewCast);
                    if (edge.PointA != Vector3.zero)
                    {
                        viewPoints[viewPointsIndex] = edge.PointA;
                        viewPointsIndex++;
                    }
                    if (edge.PointB != Vector3.zero)
                    {
                        viewPoints[viewPointsIndex] = edge.PointB;
                        viewPointsIndex++;
                    }
                }

                viewPoints[viewPointsIndex] = newViewCast.Point;
                viewPointsIndex++;
                oldViewCast = newViewCast;
            }
        }

        private EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
        {
            float minAngle = minViewCast.Angle;
            float maxAngle = maxViewCast.Angle;
            Vector3 minPoint = Vector3.zero;
            Vector3 maxPoint = Vector3.zero;

            for (int i = 0; i < edgeResolveIterations; i++)
            {
                float angle = (minAngle + maxAngle) / 2;
                ViewCastInfo newViewCast = ViewCast(angle);

                bool edgeDistanceThresholdExceeded =
                    Mathf.Abs(minViewCast.Distance - newViewCast.Distance) > edgeDistanceThreshold;
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

        private void ViewCastBatch(float[] angles, ViewCastInfo[] resultArray)
        {
            if (resultArray.Length < angles.Length)
            {
                throw new ArgumentException("Results can't be smaller than angles", nameof(resultArray));
            }
            
            // Allocate arrays for raycast data
            var hits = new NativeArray<RaycastHit>(angles.Length, Allocator.TempJob);
            var commands = new NativeArray<RaycastCommand>(angles.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            
            Vector3 origin = target.transform.position;

            // Create raycast commands
            for (var i = 0; i < angles.Length; i++)
            {
                commands[i] = new RaycastCommand(origin, DirectionFromAngle(angles[i], true), viewRange, obstacleMask);
            }

            // Schedule raycasts
            JobHandle handle = RaycastCommand.ScheduleBatch(commands, hits, 1);
            // Wait for the raycasting to complete
            handle.Complete();
            
            // Fill results array
            for (var i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                // Collider is only valid if hit (yes, this is in the docs)
                if (hit.collider)
                {
                    resultArray[i] = new ViewCastInfo(true, hit.point, hit.distance, angles[i], hit.normal);
                }
                else
                {
                    resultArray[i] = new ViewCastInfo(false, 
                        origin + DirectionFromAngle(angles[i], true) * viewRange,
                        viewRange,
                        angles[i],
                        hit.normal);
                }
            }
            
            // Dispose raycast data
            hits.Dispose();
            commands.Dispose();
        }
        
        private ViewCastInfo ViewCast(float globalAngle)
        {
            Vector3 dir = DirectionFromAngle(globalAngle, true);

            if (Physics.Raycast(target.transform.position, dir, out var hit, viewRange, obstacleMask))
            {
                return new ViewCastInfo(true, hit.point, hit.distance,
                    globalAngle,
                    hit.normal);
            }

            return new ViewCastInfo(false, target.transform.position + dir * viewRange, viewRange,
                globalAngle, hit.normal);
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
}