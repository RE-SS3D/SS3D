using System;
using System.Collections.Generic;
using UnityEngine;

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

        [HideInInspector]
        public List<Vector3> viewPoints = new List<Vector3>(MAX_VERTICES / 2);

        private List<Vector3> vertices = new List<Vector3>(MAX_VERTICES);
        private List<int> triangles = new List<int>(MAX_VERTICES * 3);

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
            viewMeshFilter = GetComponent<MeshFilter>();

            viewMesh = new Mesh();
            viewMesh.name = "View Mesh";
            viewMeshFilter.mesh = viewMesh;
        }

        private void Update()
        {
            transform.position = target.position;
            DrawFieldOfView();
        }

        public Vector3 DirectionFromAngle(float angleInDegrees, bool angleIsGlobal)
        {
            if (!angleIsGlobal)
            {
                angleInDegrees += transform.eulerAngles.y;
            }

            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }

        private void AddNeededItems(List<Vector3> list, int required)
        {
            int originalCount = list.Count;
            if (required > originalCount)
            {
                for (int i = 0; i < required - originalCount; i++)
                {
                    list.Add(new Vector3());
                }
            }
        }

        private void AddNeededItems(List<int> list, int required)
        {
            int originalCount = list.Count;
            if (required > originalCount)
            {
                for (int i = 0; i < required - originalCount; i++)
                {
                    list.Add(0);
                }
            }
        }

        public void DrawFieldOfView()
        {
            CalculateViewPoints();

            int vertexCount = viewPoints.Count + 1;
            vertices.Clear();
            triangles.Clear();
            AddNeededItems(vertices, vertexCount);
            AddNeededItems(triangles, (vertexCount - 2) * 3);

            vertices[0] = transform.InverseTransformPoint(target.transform.position);

            for (int i = 0; i < vertexCount - 1; i++)
            {
                vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);

                if (i < vertexCount - 2)
                {
                    triangles[i * 3] = 0;
                    triangles[i * 3 + 1] = i + 1;
                    triangles[i * 3 + 2] = i + 2;
                }
            }

            viewMesh.Clear();
            viewMesh.SetVertices(vertices);
            viewMesh.SetTriangles(triangles, 0);
        }

        public void CalculateViewPoints()
        {
            int stepCount = Mathf.RoundToInt(viewConeWidth * meshResolution);
            float stepAngleSize = viewConeWidth / stepCount;

            int pointCount = 0;

            viewPoints.Clear();
            ViewCastInfo oldViewCast = new ViewCastInfo();
            for (int i = 0; i <= stepCount; i++)
            {
                float angle = transform.eulerAngles.y - viewConeWidth / 2 + stepAngleSize * i;
                ViewCastInfo newViewCast = ViewCast(angle);

                bool edgeDistanceThresholdExceeded =
                    Mathf.Abs(oldViewCast.Distance - newViewCast.Distance) > edgeDistanceThreshold;
                if (oldViewCast.Hit != newViewCast.Hit ||
                    (oldViewCast.Hit && newViewCast.Hit && oldViewCast.Normal != newViewCast.Normal &&
                     edgeDistanceThresholdExceeded))
                {
                    EdgeInfo edge = FindEdge(oldViewCast, newViewCast);
                    if (edge.PointA != Vector3.zero) viewPoints.Add(edge.PointA);
                    if (edge.PointB != Vector3.zero) viewPoints.Add(edge.PointB);
                }

                pointCount++;
                AddNeededItems(viewPoints, pointCount);
                viewPoints.Add(newViewCast.Point);
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