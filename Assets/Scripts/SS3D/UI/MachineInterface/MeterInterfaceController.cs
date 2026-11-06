using FishNet.Connection;
using FishNet.Object;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Read-only panel for a pipe meter. Open to anyone: there's nothing on it to lock.
    /// </summary>
    [RequireComponent(typeof(AtmosMeterController))]
    public sealed class MeterInterfaceController : MachineInterfaceBehaviour
    {
        private const float LitresPerCubicMetre = 1000f;

        [SerializeField]
        private string _modelLabel = "GM-02 · pipe gas meter";

        [SerializeField]
        private string _deviceTitle = "GM-02";

        private readonly float[] _history = new float[AtmosMeterController.HistoryLength];
        private AtmosMeterController _meter;

        private AtmosMeterController Meter => _meter != null ? _meter : _meter = GetComponent<AtmosMeterController>();

        public override string InterfaceId => MachineInterfaceIds.Meter;

        protected override bool ApplyControl(byte controlId, bool value) => false;

        protected override void SendOpenToViewer(NetworkConnection conn)
        {
            TargetOpenInterface(conn, BuildSnapshot());
        }

        protected override void SendRefreshToViewer(NetworkConnection conn)
        {
            TargetRefreshInterface(conn, BuildSnapshot());
        }

        [TargetRpc(RunLocally = true)]
        private void TargetOpenInterface(NetworkConnection conn, MeterInterfaceSnapshot snapshot)
        {
            DispatchClientOpen(conn, snapshot);
        }

        [TargetRpc(RunLocally = true)]
        private void TargetRefreshInterface(NetworkConnection conn, MeterInterfaceSnapshot snapshot)
        {
            DispatchClientRefresh(conn, snapshot);
        }

        private MeterInterfaceSnapshot BuildSnapshot()
        {
            GasPanelRow[] gases = GasPanelReadings.BuildRows();
            GasPipeNetworkRecord line = null;
            bool connected = Meter != null && Meter.TryGetLine(out line);
            Meter?.CopyHistory(_history);

            return new MeterInterfaceSnapshot
            {
                MachineObjectId = NetworkObject != null ? NetworkObject.ObjectId : 0,
                InterfaceId = InterfaceId,
                ModelLabel = _modelLabel,
                DeviceTitle = _deviceTitle,
                LineLabel = connected ? FormatLine(line) : string.Empty,
                Connected = connected,
                TemperatureKelvin = connected ? line.Temperature : 0f,
                Moles = connected ? line.GetTotalMoles(AtmosConstants.DefaultGasCount) : 0f,
                VolumeLitres = connected ? line.Volume * LitresPerCubicMetre : 0f,
                Gases = gases,
                Line = GasPanelReadings.Read(line, gases),
                History = _history,
            };
        }

        private static string FormatLine(GasPipeNetworkRecord line)
        {
            string name = "Pipe";
            foreach (GasPipeSegmentKey segment in line.Segments)
            {
                name = segment.Layer switch
                {
                    TileLayer.PipeLeft => "Supply",
                    TileLayer.PipeMiddle => "Mixed",
                    TileLayer.PipeRight => "Waste",
                    TileLayer.PipeSurface => "Surface",
                    _ => name,
                };
                break;
            }

            int segments = line.Segments.Count;
            return $"{name} line · {segments} segment{(segments == 1 ? string.Empty : "s")}";
        }
    }
}
