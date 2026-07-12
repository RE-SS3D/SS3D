using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using System.Collections.Generic;
using System.Electricity;
using UnityEngine;

namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// Draws circuit membership, cable links, and per-device power diagnostics in the Scene view.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ElectricityDebugGizmoDrawer : Actor
    {
        private const float LabelHeight = 0.65f;
        private const float LinkHeight = 0.18f;
        private const float NodeRadius = 0.16f;

        protected override void OnStart()
        {
            base.OnStart();

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            Destroy(this);
#endif
        }

        private void OnDrawGizmos()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            return;
#endif
            if (!ElectricityDevSettings.ShowElectricityGizmos || !UnityEngine.Application.isPlaying)
            {
                return;
            }

            if (!SubSystems.TryGet(out ElectricitySubSystem electricitySubSystem)
                || !SubSystems.TryGet(out TileSubSystem tileSubSystem))
            {
                return;
            }

            ITileQueryService query = tileSubSystem.QueryService;
            if (query == null)
            {
                return;
            }

#if UNITY_EDITOR
            UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
#endif

            IReadOnlyList<ElectricityDeviceDebugInfo> devices = electricitySubSystem.CollectDeviceDebugInfo();
            DrawDeviceLinks(devices, query);
            DrawDeviceLabels(devices);
        }

        private static void DrawDeviceLinks(IReadOnlyList<ElectricityDeviceDebugInfo> devices, ITileQueryService query)
        {
            var positions = new Dictionary<PlacedTileObject, Vector3>();
            foreach (ElectricityDeviceDebugInfo device in devices)
            {
                if (device.TileObject == null)
                {
                    continue;
                }

                positions[device.TileObject] = query.TileToWorld(new TileCoord(device.TileObject.MapId, device.TileObject.WorldOrigin))
                    + new Vector3(0f, LinkHeight, 0f);
            }

            var drawn = new HashSet<(int, int)>();
            foreach (ElectricityDeviceDebugInfo device in devices)
            {
                if (device.TileObject == null || !positions.TryGetValue(device.TileObject, out Vector3 from))
                {
                    continue;
                }

                foreach (PlacedTileObject neighbour in ElectricNeighbourLookup.GetNeighbours(device.TileObject))
                {
                    if (neighbour == null || !positions.TryGetValue(neighbour, out Vector3 to))
                    {
                        continue;
                    }

                    int a = device.TileObject.GetInstanceID();
                    int b = neighbour.GetInstanceID();
                    (int, int) key = a < b ? (a, b) : (b, a);
                    if (!drawn.Add(key))
                    {
                        continue;
                    }

                    Color color = device.InCircuit
                        ? ElectricityDevSettings.GetColorForCircuit(device.CircuitIndex)
                        : Color.gray;
                    color.a = 0.55f;
                    Gizmos.color = color;
                    Gizmos.DrawLine(from, to);
                }
            }
        }

        private static void DrawDeviceLabels(IReadOnlyList<ElectricityDeviceDebugInfo> devices)
        {
            foreach (ElectricityDeviceDebugInfo device in devices)
            {
                if (device.TileObject == null)
                {
                    continue;
                }

                Color color = device.InCircuit
                    ? ElectricityDevSettings.GetColorForCircuit(device.CircuitIndex)
                    : Color.gray;

                Gizmos.color = color;
                Gizmos.DrawSphere(device.WorldPosition + new Vector3(0f, LinkHeight, 0f), NodeRadius);

#if UNITY_EDITOR
                UnityEditor.Handles.color = color;
                UnityEditor.Handles.Label(device.WorldPosition + new Vector3(0f, LabelHeight, 0f), device.Label);
#endif
            }
        }
    }
}
