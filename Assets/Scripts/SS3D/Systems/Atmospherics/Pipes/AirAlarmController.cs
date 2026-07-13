using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Systems.Area;
using SS3D.Systems.Examine;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using System.Collections.Generic;
using System.Electricity;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Pipes
{
    [System.Flags]
    public enum AirAlarmState : byte
    {
        None = 0,
        LowOxygen = 1 << 0,
        HighCarbonDioxide = 1 << 1,
        HighPressure = 1 << 2,
        LowPressure = 1 << 3,
    }

    /// <summary>
    /// Area-scoped turf monitor with networked alarm state (design §6 foundation).
    /// </summary>
    [RequireComponent(typeof(PlacedTileObject))]
    [RequireComponent(typeof(BasicPowerConsumer))]
    [RequireComponent(typeof(ElectricDeviceAdjacencyConnector))]
    public sealed class AirAlarmController : NetworkBehaviour
    {
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int LuminId = Shader.PropertyToID("_Lumin");

        [SerializeField]
        private BasicPowerConsumer _powerConsumer;

        [SerializeField]
        private Renderer[] _emissiveRenderers;

        [SyncVar(OnChange = nameof(HandleAlarmStateChanged))]
        private AirAlarmState _alarmState;

        [SyncVar]
        private float _samplePressureKpa;

        [SyncVar]
        private float _sampleOxygenFraction;

        [SyncVar]
        private float _sampleCarbonDioxideFraction;

        private PlacedTileObject _tileObject;
        private AreaId _areaId;
        private bool _hasArea;
        private readonly List<Material> _emissiveMaterials = new();
        private float _poweredLumin;
        private Color _poweredEmission;
        private Color _alarmEmission = new(1f, 0.15f, 0.1f, 1f);
        private bool _powerEventsSubscribed;

        public AirAlarmState AlarmState => _alarmState;

        public float SamplePressureKpa => _samplePressureKpa;

        public float SampleOxygenFraction => _sampleOxygenFraction;

        public float SampleCarbonDioxideFraction => _sampleCarbonDioxideFraction;

        public override void OnStartServer()
        {
            base.OnStartServer();
            Initialize();
            InvokeRepeating(nameof(ServerSampleTick), AtmosConstants.TickInterval, AtmosConstants.TickInterval);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Initialize();
            CacheEmissiveMaterials();
            RefreshVisuals();
        }

        public override void OnStopServer()
        {
            CancelInvoke(nameof(ServerSampleTick));
            base.OnStopServer();
        }

        private void OnDestroy()
        {
            UnsubscribePowerEvents();
        }

        [Server]
        private void ServerSampleTick()
        {
            ResolveArea();
            if (!_hasArea || !IsPowered())
            {
                _alarmState = AirAlarmState.None;
                return;
            }

            if (!SubSystems.TryGet(out TileSubSystem tileSubSystem)
                || !SubSystems.TryGet(out AtmosSubSystem atmosSubSystem)
                || tileSubSystem.CurrentMap == null
                || atmosSubSystem.Simulation == null)
            {
                _alarmState = AirAlarmState.None;
                return;
            }

            if (!AtmosAreaSampler.TrySampleArea(
                    tileSubSystem.CurrentMap,
                    tileSubSystem.QueryService,
                    _areaId,
                    atmosSubSystem.Simulation,
                    out AtmosAreaSample sample))
            {
                _alarmState = AirAlarmState.None;
                return;
            }

            _samplePressureKpa = sample.AveragePressureKpa;
            _sampleOxygenFraction = sample.OxygenMoleFraction;
            _sampleCarbonDioxideFraction = sample.CarbonDioxideMoleFraction;

            AirAlarmState next = AirAlarmState.None;
            if (sample.OxygenMoleFraction < AirAlarmConstants.LowOxygenMoleFraction)
                next |= AirAlarmState.LowOxygen;
            if (sample.CarbonDioxideMoleFraction > AirAlarmConstants.HighCarbonDioxideMoleFraction)
                next |= AirAlarmState.HighCarbonDioxide;
            if (sample.AveragePressureKpa > AirAlarmConstants.HighPressureKpa)
                next |= AirAlarmState.HighPressure;
            if (sample.AveragePressureKpa < AirAlarmConstants.LowPressureKpa)
                next |= AirAlarmState.LowPressure;

            _alarmState = next;
        }

        public void AppendExamineSections(List<ExamineSection> sections)
        {
            if (sections == null)
                return;

            if (!IsPowered())
            {
                sections.Add(new ExamineSection("Air alarm: unpowered — environmental scan offline."));
                return;
            }

            if (!_hasArea)
            {
                sections.Add(new ExamineSection("Air alarm: no area assigned — cannot sample environment."));
                return;
            }

            string status = _alarmState == AirAlarmState.None ? "Nominal" : _alarmState.ToString();
            sections.Add(new ExamineSection(
                $"Air alarm: {status}. Pressure {_samplePressureKpa:F1} kPa. " +
                $"O₂ {_sampleOxygenFraction:P1}. CO₂ {_sampleCarbonDioxideFraction:P2}."));
        }

        private void Initialize()
        {
            if (_tileObject == null)
                TryGetComponent(out _tileObject);

            if (_powerConsumer == null)
                TryGetComponent(out _powerConsumer);

            SubscribePowerEvents();
            ResolveArea();
        }

        private void ResolveArea()
        {
            _hasArea = false;
            if (_tileObject == null || !SubSystems.TryGet(out AreaSubSystem areaSubSystem))
                return;

            if (!areaSubSystem.TryGetAreaForDevice(_tileObject, out AreaRecord record))
                return;

            _areaId = record.Id;
            _hasArea = true;
        }

        private void SubscribePowerEvents()
        {
            if (_powerEventsSubscribed || _powerConsumer == null)
                return;

            _powerConsumer.OnPowerStatusUpdated += HandlePowerStatusUpdated;
            _powerEventsSubscribed = true;
        }

        private void UnsubscribePowerEvents()
        {
            if (!_powerEventsSubscribed || _powerConsumer == null)
                return;

            _powerConsumer.OnPowerStatusUpdated -= HandlePowerStatusUpdated;
            _powerEventsSubscribed = false;
        }

        private void HandlePowerStatusUpdated(object sender, PowerStatus status)
        {
            RefreshVisuals();
        }

        private void HandleAlarmStateChanged(AirAlarmState _, AirAlarmState __, bool asServer)
        {
            if (!asServer)
                RefreshVisuals();
        }

        private void CacheEmissiveMaterials()
        {
            _emissiveMaterials.Clear();
            if (_emissiveRenderers == null)
                return;

            foreach (Renderer renderer in _emissiveRenderers)
            {
                if (renderer == null)
                    continue;

                Material[] materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (material == null || !material.IsKeywordEnabled("_EMISSION"))
                        continue;

                    _emissiveMaterials.Add(material);
                    if (_poweredLumin <= 0f)
                    {
                        _poweredLumin = material.GetFloat(LuminId);
                        _poweredEmission = material.GetColor(EmissionColorId);
                    }
                }
            }
        }

        private void RefreshVisuals()
        {
            bool powered = IsPowered();
            bool alarming = _alarmState != AirAlarmState.None;
            float lumin = powered ? (alarming ? _poweredLumin : _poweredLumin * 0.35f) : 0f;
            Color emission = powered
                ? (alarming ? _alarmEmission : _poweredEmission * 0.35f)
                : Color.black;

            foreach (Material material in _emissiveMaterials)
            {
                material.SetFloat(LuminId, lumin);
                material.SetColor(EmissionColorId, emission);
            }
        }

        private bool IsPowered() => AtmosPortPower.IsPowered(_powerConsumer);
    }
}
