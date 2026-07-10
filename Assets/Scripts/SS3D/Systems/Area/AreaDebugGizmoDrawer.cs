using System.Collections.Generic;
using System.Electricity;
using System.Text;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using UnityEngine;

namespace SS3D.Systems.Area
{
    /// <summary>
    /// Draws per-tile area flood-fill overlays and APC/device diagnostics in the Scene view during play mode.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AreaDebugGizmoDrawer : Actor
    {
        private const float TileYOffset = 0.22f;
        private const float TileThickness = 0.04f;
        private const float TileInset = 0.9f;
        private const float DeviceLabelHeight = 0.55f;
        private const float DeviceMarkerRadius = 0.18f;

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
            if (!AreaDevSettings.ShowAreaGizmos || !UnityEngine.Application.isPlaying)
            {
                return;
            }

            if (!SubSystems.TryGet(out TileSubSystem tileSubSystem))
            {
                return;
            }

            TileMap map = tileSubSystem.CurrentMap;
            ITileQueryService query = tileSubSystem.QueryService;
            if (map == null || query == null)
            {
                return;
            }

            SubSystems.TryGet(out AreaSubSystem areaSubSystem);

#if UNITY_EDITOR
            UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
#endif

            DrawTileOverlays(map, query);

            if (areaSubSystem == null)
            {
                return;
            }

            DrawApcAndDeviceLabels(areaSubSystem, query);
        }

        private static void DrawTileOverlays(TileMap map, ITileQueryService query)
        {
            foreach (TileChunk chunk in map.GetAllChunks())
            {
                if (!chunk.HasAnyAreaIds())
                {
                    continue;
                }

                for (int x = 0; x < TileChunk.ChunkSize; x++)
                {
                    for (int y = 0; y < TileChunk.ChunkSize; y++)
                    {
                        Vector3 world = chunk.GetWorldPosition(x, y);
                        TileCoord coord = query.WorldToTile(world, map.MapId);
                        if (!map.TryGetAreaId(coord, out ushort areaId) || areaId == AreaId.None)
                        {
                            continue;
                        }

                        DrawTileOverlay(world, areaId);
                    }
                }
            }
        }

        private static void DrawApcAndDeviceLabels(AreaSubSystem areaSubSystem, ITileQueryService query)
        {
            Dictionary<AreaId, List<DeviceDebugInfo>> devicesByArea = CollectDevicesByArea(areaSubSystem, query);
            List<DeviceDebugInfo> unassignedDevices = CollectUnassignedDevices(areaSubSystem, query);

            foreach (AreaRecord record in areaSubSystem.GetAllAreas())
            {
                if (record.Apc == null)
                {
                    continue;
                }

                Color areaColor = AreaDevSettings.GetColorForArea(record.Id.Value);
                Vector3 apcPosition = query.TileToWorld(record.Apc.OriginTile) + new Vector3(0f, 0.35f, 0f);

                Gizmos.color = areaColor;
                Gizmos.DrawWireSphere(apcPosition, 0.35f);

                Vector2Int facingOffset = TileHelper.CoordinateDifferenceInFrontFacingDirection(record.Apc.FacingDirection);
                Vector3 facingEnd = apcPosition + new Vector3(facingOffset.x, 0f, facingOffset.y);
                Gizmos.DrawLine(apcPosition, facingEnd);

                devicesByArea.TryGetValue(record.Id, out List<DeviceDebugInfo> devices);
                int deviceCount = devices?.Count ?? 0;

#if UNITY_EDITOR
                string apcLabel = BuildApcLabel(record, deviceCount);
                areaSubSystem.TryGetLightingState(record.Id, out AreaLightingState lightingState);
                apcLabel += $"\nLighting: {lightingState}";

                UnityEditor.Handles.color = areaColor;
                UnityEditor.Handles.Label(apcPosition + Vector3.up * 0.5f, apcLabel);
#endif

                if (devices == null)
                {
                    continue;
                }

                foreach (DeviceDebugInfo device in devices)
                {
                    Gizmos.color = areaColor;
                    Gizmos.DrawWireSphere(device.Position, DeviceMarkerRadius);
                    Gizmos.DrawLine(apcPosition, device.Position);

#if UNITY_EDITOR
                    UnityEditor.Handles.color = device.IsLightFixture && !device.WouldEmitLight
                        ? Color.gray
                        : areaColor;
                    UnityEditor.Handles.Label(device.Position + Vector3.up * DeviceLabelHeight, device.Label);
#endif
                }
            }

#if UNITY_EDITOR
            foreach (DeviceDebugInfo device in unassignedDevices)
            {
                Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
                Gizmos.DrawWireSphere(device.Position, DeviceMarkerRadius);

                UnityEditor.Handles.color = new Color(1f, 0.85f, 0.2f);
                UnityEditor.Handles.Label(device.Position + Vector3.up * DeviceLabelHeight, device.Label);
            }
#endif
        }

        private static Dictionary<AreaId, List<DeviceDebugInfo>> CollectDevicesByArea(
            AreaSubSystem areaSubSystem,
            ITileQueryService query)
        {
            var devicesByArea = new Dictionary<AreaId, List<DeviceDebugInfo>>();

            foreach (BasicElectricDevice device in UnityEngine.Object.FindObjectsByType<BasicElectricDevice>(FindObjectsSortMode.None))
            {
                if (!ShouldIncludeDevice(device))
                {
                    continue;
                }

                if (!TryBuildDeviceDebugInfo(device, areaSubSystem, query, out DeviceDebugInfo info) || !info.HasArea)
                {
                    continue;
                }

                if (!devicesByArea.TryGetValue(info.AreaId, out List<DeviceDebugInfo> list))
                {
                    list = new List<DeviceDebugInfo>();
                    devicesByArea[info.AreaId] = list;
                }

                list.Add(info);
            }

            return devicesByArea;
        }

        private static List<DeviceDebugInfo> CollectUnassignedDevices(AreaSubSystem areaSubSystem, ITileQueryService query)
        {
            var unassigned = new List<DeviceDebugInfo>();

            foreach (BasicElectricDevice device in UnityEngine.Object.FindObjectsByType<BasicElectricDevice>(FindObjectsSortMode.None))
            {
                if (!ShouldIncludeDevice(device))
                {
                    continue;
                }

                if (!TryBuildDeviceDebugInfo(device, areaSubSystem, query, out DeviceDebugInfo info) || info.HasArea)
                {
                    continue;
                }

                unassigned.Add(info);
            }

            return unassigned;
        }

        private static bool ShouldIncludeDevice(BasicElectricDevice device)
        {
            if (device is IApcChannelSource)
            {
                return false;
            }

            PlacedTileObject tileObject = device.TileObject;
            if (tileObject == null || IsCableOrWire(tileObject))
            {
                return false;
            }

            return device is IPowerConsumer || device.TryGetComponent(out LightPower _);
        }

        private static bool IsCableOrWire(PlacedTileObject tileObject)
        {
            if (tileObject.Connector is CablesAdjacencyConnector)
            {
                return true;
            }

            return tileObject.GenericType is TileObjectGenericType.Cable or TileObjectGenericType.Wire;
        }

        private static bool TryBuildDeviceDebugInfo(
            BasicElectricDevice device,
            AreaSubSystem areaSubSystem,
            ITileQueryService query,
            out DeviceDebugInfo info)
        {
            info = default;
            PlacedTileObject tileObject = device.TileObject;
            if (tileObject == null)
            {
                return false;
            }

            TileCoord origin = AreaDeviceTileResolver.GetOriginTile(tileObject);
            TileCoord lookupTile = origin;
            bool hasArea = areaSubSystem.TryGetAreaForTile(origin, out AreaRecord record);
            if (!hasArea)
            {
                lookupTile = AreaDeviceTileResolver.GetTileInFront(tileObject);
                hasArea = areaSubSystem.TryGetAreaForTile(lookupTile, out record);
            }

            info.Position = query.TileToWorld(origin) + new Vector3(0f, DeviceLabelHeight, 0f);
            info.HasArea = hasArea;
            info.AreaId = hasArea ? record.Id : default;
            info.IsLightFixture = device.TryGetComponent(out LightPower lightPower);

            AreaLightingState lightingState = ResolveLightingState(areaSubSystem, hasArea, record);

            if (info.IsLightFixture)
            {
                PowerStatus powerStatus = device is IPowerConsumer consumer
                    ? consumer.PowerStatus
                    : PowerStatus.Inactive;
                PowerStatus effectivePower = LightingDevBypass.IsActive ? PowerStatus.Powered : powerStatus;
                info.WouldEmitLight = AreaLightFixturePolicy.ShouldEmitLight(
                    hasArea,
                    lightingState,
                    lightPower.FixtureCapability,
                    effectivePower,
                    out bool emergencyVisuals);
                info.UsesEmergencyVisuals = emergencyVisuals;
            }

            info.Label = BuildDeviceLabel(device, hasArea, origin, lookupTile, lightingState, info);
            return true;
        }

        private static AreaLightingState ResolveLightingState(
            AreaSubSystem areaSubSystem,
            bool hasArea,
            AreaRecord record)
        {
            if (!hasArea)
            {
                return AreaLightingState.Dark;
            }

            if (areaSubSystem.TryGetLightingState(record.Id, out AreaLightingState derivedState))
            {
                return derivedState;
            }

            return AreaLightingState.Normal;
        }

        private static string BuildDeviceLabel(
            BasicElectricDevice device,
            bool hasArea,
            TileCoord origin,
            TileCoord lookupTile,
            AreaLightingState lightingState,
            DeviceDebugInfo info)
        {
            var label = new StringBuilder();
            label.Append(device.gameObject.name);
            label.Append('\n');
            label.Append(GetDeviceKindLabel(device));

            if (!hasArea)
            {
                label.Append("\nNo area · origin ");
                label.Append(origin.Grid.x);
                label.Append(',');
                label.Append(origin.Grid.y);
                if (lookupTile.MapId != origin.MapId || lookupTile.Grid != origin.Grid)
                {
                    label.Append(" · front ");
                    label.Append(lookupTile.Grid.x);
                    label.Append(',');
                    label.Append(lookupTile.Grid.y);
                }

                return label.ToString();
            }

            label.Append('\n');

            if (info.IsLightFixture)
            {
                label.Append(info.WouldEmitLight ? "Emit" : "Off");
                if (info.UsesEmergencyVisuals)
                {
                    label.Append(" (emergency)");
                }

                if (device is IPowerConsumer consumer)
                {
                    label.Append(" · ");
                    label.Append(consumer.PowerStatus);
                }

                label.Append(" · ");
                label.Append(lightingState);
            }
            else if (device is IPowerConsumer consumer)
            {
                label.Append(consumer.PowerStatus);
                label.Append(" · ");
                label.Append(consumer.Channel);
                label.Append(" · ");
                label.Append(consumer.PowerNeeded.ToString("0.##"));
                label.Append(" kW · area ");
                label.Append(lightingState);
            }

            label.Append("\nlookup ");
            label.Append(lookupTile.Grid.x);
            label.Append(',');
            label.Append(lookupTile.Grid.y);

            if (LightingDevBypass.IsActive && info.IsLightFixture)
            {
                label.Append(" · dev bypass");
            }

            return label.ToString();
        }

        private static string GetDeviceKindLabel(BasicElectricDevice device)
        {
            if (device.TryGetComponent(out LightPower _))
            {
                return "Light fixture";
            }

            if (device is MachinePowerConsumer)
            {
                return "Machine load";
            }

            return device is IPowerConsumer ? "Power consumer" : "Electric device";
        }

        private static string BuildApcLabel(AreaRecord record, int deviceCount)
        {
            string name = string.IsNullOrWhiteSpace(record.DisplayName)
                ? $"Area {record.Id.Value}"
                : $"{record.DisplayName} ({record.Id.Value})";

            return deviceCount > 0 ? $"{name}\n{deviceCount} load(s)" : name;
        }

        private static void DrawTileOverlay(Vector3 world, ushort areaId)
        {
            Vector3 center = world + new Vector3(0f, TileYOffset, 0f);
            Vector3 size = new Vector3(TileInset, TileThickness, TileInset);
            Color color = AreaDevSettings.GetColorForArea(areaId);

            Gizmos.color = color;
            Gizmos.DrawCube(center, size);

            Color wireColor = color;
            wireColor.a = 1f;
            Gizmos.color = wireColor;
            Gizmos.DrawWireCube(center, size);
        }

        private struct DeviceDebugInfo
        {
            public Vector3 Position;

            public AreaId AreaId;

            public bool HasArea;

            public bool IsLightFixture;

            public bool WouldEmitLight;

            public bool UsesEmergencyVisuals;

            public string Label;
        }
    }
}
