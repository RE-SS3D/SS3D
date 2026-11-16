using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Systems.Audio;
using SS3D.Core;
using SS3D.Data.Persistence;
using SS3D.Systems.Area;
using SS3D.Systems.Examine;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using System.Collections.Generic;
using SS3D.Systems.Electricity;
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
        HighPlasma = 1 << 4,
        HighTemperature = 1 << 5,
    }

    /// <summary>
    /// Area-scoped turf monitor with networked alarm state (design §6 foundation).
    /// </summary>
    [RequireComponent(typeof(PlacedTileObject))]
    [RequireComponent(typeof(BasicPowerConsumer))]
    [RequireComponent(typeof(ElectricDeviceAdjacencyConnector))]
    public sealed class AirAlarmController : NetworkBehaviour, IAmbientLoopState, IAmbientLoopArea, IPersistentSetup, ISetupScope
    {
        /// <summary>Seconds lit and seconds dark for a dangerous room: a fast flash.</summary>
        private const float DangerFlashSeconds = 0.25f;

        /// <summary>Seconds lit and seconds dark while the room fills or drains: a slow blink.</summary>
        private const float TrendBlinkSeconds = 0.6f;

        private static readonly Color NominalEmission = new(0.25f, 2.2f, 0.45f, 1f);
        private static readonly Color RepressurizingEmission = new(0.3f, 0.9f, 3f, 1f);
        private static readonly Color AlarmEmission = new(3f, 0.3f, 0.2f, 1f);

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField]
        private BasicPowerConsumer _powerConsumer;

        [SerializeField]
        private Renderer[] _emissiveRenderers;

        [SyncVar]
        private AirAlarmState _alarmState;

        [SyncVar(OnChange = nameof(HandleIndicatorChanged))]
        private AirAlarmIndicator _indicator;

        [SyncVar]
        private float _samplePressureKpa;

        [SyncVar]
        private float _sampleOxygenFraction;

        [SyncVar]
        private float _sampleCarbonDioxideFraction;

        [SyncVar]
        private float _sampleNitrogenFraction;

        [SyncVar]
        private float _samplePlasmaFraction;

        [SyncVar]
        private float _sampleTemperatureKelvin;

        [SyncVar]
        private bool _hasSample;

        [SyncVar]
        private byte _activeMode = (byte)AirAlarmPresetMode.Filtering;

        private PlacedTileObject _tileObject;
        private AreaId _areaId;
        private bool _hasArea;
        private EmissiveMaterialSlots _emissiveSlots;
        private bool _powerEventsSubscribed;
        private float _blinkSeconds;
        private Color _blinkEmission;
        private bool _blinkLit;
        private bool _hasTrendSample;
        private float _trendPressureKpa;
        private float _smoothedRateKpaPerSecond;
        private AirAlarmPressureTrend _trend;

        public AirAlarmState AlarmState => _alarmState;

        /// <summary>The face's state, which also tells the panel whether the room is filling or draining.</summary>
        public AirAlarmIndicator Indicator => _indicator;

        /// <summary>The siren sounds for danger and leaks, not while vents refill a room.</summary>
        public bool IsLoopRunning =>
            (_indicator == AirAlarmIndicator.Danger || _indicator == AirAlarmIndicator.Depressurizing) && IsPowered();

        public AirAlarmPresetMode ActiveMode => (AirAlarmPresetMode)_activeMode;

        public AreaId AreaId => _areaId;

        public bool HasArea => _hasArea;

        /// <summary>
        /// The alarm sounds only in its own area (<see cref="AmbientLoopSettings.StaysInArea"/>).
        /// Resolved per call from the client floor cache, since <see cref="ResolveArea"/> reads the
        /// server registry and runs once at start, before a joining client has the floor snapshot.
        /// </summary>
        public bool TryGetLoopArea(out AreaId areaId)
        {
            areaId = default;
            if (_tileObject == null && !TryGetComponent(out _tileObject))
                return false;

            return SubSystems.TryGet(out AreaSubSystem areaSubSystem)
                && areaSubSystem.TryResolveAreaIdForDevice(_tileObject, out areaId);
        }

        public float SamplePressureKpa => _samplePressureKpa;

        public float SampleOxygenFraction => _sampleOxygenFraction;

        public float SampleCarbonDioxideFraction => _sampleCarbonDioxideFraction;

        public float SampleNitrogenFraction => _sampleNitrogenFraction;

        public float SamplePlasmaFraction => _samplePlasmaFraction;

        public float SampleTemperatureKelvin => _sampleTemperatureKelvin;

        public bool HasSample => _hasSample;

        public override void OnStartServer()
        {
            base.OnStartServer();
            Initialize();
            AtmosSubSystem.Channel.Subscribe(ServerSampleTick);
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
            AtmosSubSystem.Channel.Unsubscribe(ServerSampleTick);
            base.OnStopServer();
        }

        private void OnDestroy()
        {
            AtmosSubSystem.Channel.Unsubscribe(ServerSampleTick);
            UnsubscribePowerEvents();
        }

        public string SetupKey => "air-alarm";

        [System.Serializable]
        private sealed class AirAlarmSetup
        {
            public byte preset = (byte)AirAlarmPresetMode.Filtering;
        }

        /// <summary>The preset is the alarm's own setup; the vents and scrubbers it switched save their own settings.</summary>
        public string CaptureSetup() =>
            _activeMode == (byte)AirAlarmPresetMode.Filtering ? null : JsonUtility.ToJson(new AirAlarmSetup { preset = _activeMode });

        /// <summary>Sets the preset without dispatching it: the room's devices restore their own saved settings.</summary>
        public void RestoreSetup(string json)
        {
            AirAlarmSetup setup = new();
            if (!string.IsNullOrEmpty(json))
                JsonUtility.FromJsonOverwrite(json, setup);

            _activeMode = System.Enum.IsDefined(typeof(AirAlarmPresetMode), setup.preset)
                ? setup.preset
                : (byte)AirAlarmPresetMode.Filtering;
        }

        /// <summary>The alarm's panel switches every vent and scrubber in its room.</summary>
        public void CollectSetupScope(List<GameObject> into)
        {
            ResolveArea();
            if (_hasArea)
                AtmosAreaDeviceQuery.ForEachAreaPort(_areaId, (port, _) => into.Add(port.gameObject));
        }

        [Server]
        public void ServerSetPresetMode(AirAlarmPresetMode mode)
        {
            _activeMode = (byte)mode;
            ResolveArea();
            if (_hasArea)
            {
                AirAlarmPresetDispatcher.ApplyPreset(_areaId, mode);
            }
        }

        [Server]
        public void ServerSetPortEnabled(int objectId, bool enabled)
        {
            if (!AtmosAreaDeviceQuery.TryResolvePort(objectId, out AtmosPortControllerBase controller, out _))
            {
                return;
            }

            if (!IsPortInAlarmArea(controller))
            {
                return;
            }

            controller.ServerSetEnabled(enabled);
        }

        [Server]
        public void ServerSetVentTargetPressure(int objectId, float targetKpa)
        {
            if (!AtmosAreaDeviceQuery.TryResolvePort(objectId, out AtmosPortControllerBase controller, out _)
                || controller is not VentController vent)
            {
                return;
            }

            if (!IsPortInAlarmArea(controller))
            {
                return;
            }

            vent.ServerSetTargetPressureKpa(Mathf.RoundToInt(targetKpa));
        }

        [Server]
        public void ServerToggleScrubberFilter(int objectId, int filterIndex)
        {
            if (!AtmosAreaDeviceQuery.TryResolvePort(objectId, out AtmosPortControllerBase controller, out _)
                || controller is not ScrubberController scrubber)
            {
                return;
            }

            if (!IsPortInAlarmArea(controller))
            {
                return;
            }

            scrubber.ServerToggleFilter(filterIndex);
        }

        [Server]
        private void ServerSampleTick()
        {
            ResolveArea();
            if (!_hasArea || !IsPowered())
            {
                ClearSample(IsPowered());
                return;
            }

            if (!SubSystems.TryGet(out TileSubSystem tileSubSystem)
                || !SubSystems.TryGet(out AtmosSubSystem atmosSubSystem)
                || atmosSubSystem.Simulation == null
                || _tileObject == null)
            {
                ClearSample(true);
                return;
            }

            TileCoord sampleCoord = AreaDeviceTileResolver.GetTileInFront(_tileObject);
            if (tileSubSystem.QueryService != null
                && !AreaBoundaryEvaluator.IsWalkable(tileSubSystem.QueryService, sampleCoord)
                && AreaBoundaryEvaluator.IsWalkable(tileSubSystem.QueryService, AreaDeviceTileResolver.GetOriginTile(_tileObject)))
            {
                sampleCoord = AreaDeviceTileResolver.GetOriginTile(_tileObject);
            }

            if (!AtmosAreaSampler.TrySampleTile(sampleCoord, atmosSubSystem.Simulation, out AtmosAreaSample sample))
            {
                ClearSample(true);
                return;
            }

            _hasSample = true;
            _samplePressureKpa = sample.AveragePressureKpa;
            _sampleOxygenFraction = sample.OxygenMoleFraction;
            _sampleNitrogenFraction = sample.NitrogenMoleFraction;
            _sampleCarbonDioxideFraction = sample.CarbonDioxideMoleFraction;
            _samplePlasmaFraction = sample.PlasmaMoleFraction;
            _sampleTemperatureKelvin = sample.TemperatureKelvin;

            AirAlarmState next = AirAlarmState.None;
            if (sample.OxygenMoleFraction < AirAlarmConstants.LowOxygenMoleFraction)
                next |= AirAlarmState.LowOxygen;
            if (sample.CarbonDioxideMoleFraction > AirAlarmConstants.HighCarbonDioxideMoleFraction)
                next |= AirAlarmState.HighCarbonDioxide;
            if (sample.AveragePressureKpa > AirAlarmConstants.HighPressureKpa)
                next |= AirAlarmState.HighPressure;
            if (sample.AveragePressureKpa < AirAlarmConstants.LowPressureKpa)
                next |= AirAlarmState.LowPressure;
            if (sample.PlasmaMoleFraction > AirAlarmConstants.HighPlasmaMoleFraction)
                next |= AirAlarmState.HighPlasma;
            if (sample.TemperatureKelvin > AirAlarmConstants.HighTemperatureKelvin)
                next |= AirAlarmState.HighTemperature;

            _alarmState = next;
            UpdateTrend(sample.AveragePressureKpa);
            _indicator = AirAlarmIndicatorRules.Resolve(powered: true, next, _trend);
        }

        /// <summary>No reading: no flags, no trend. A powered alarm with nothing to read shows plain green.</summary>
        [Server]
        private void ClearSample(bool powered)
        {
            _alarmState = AirAlarmState.None;
            _hasSample = false;
            _hasTrendSample = false;
            _smoothedRateKpaPerSecond = 0f;
            _trend = AirAlarmPressureTrend.Steady;
            _indicator = powered ? AirAlarmIndicator.Nominal : AirAlarmIndicator.Off;
        }

        [Server]
        private void UpdateTrend(float pressureKpa)
        {
            if (_hasTrendSample)
            {
                _smoothedRateKpaPerSecond = AirAlarmIndicatorRules.SmoothRate(
                    _smoothedRateKpaPerSecond, _trendPressureKpa, pressureKpa, AtmosConstants.TickInterval);
            }

            _hasTrendSample = true;
            _trendPressureKpa = pressureKpa;
            _trend = AirAlarmIndicatorRules.ResolveTrend(_smoothedRateKpaPerSecond, _trend);
        }

        public void AppendExamineSections(List<ExamineSection> sections)
        {
            if (sections == null)
                return;

            // The power consumer's own line says it is unpowered; an unpowered alarm reads nothing more.
            if (!IsPowered())
            {
                return;
            }

            if (!_hasArea)
            {
                sections.Add(new ExamineSection("Air alarm: no area assigned — cannot sample environment."));
                return;
            }

            // Alarm state reads at range (coarse device state); the readings are fine print.
            AirAlarmStatus described = AirAlarmStatusText.Describe(true, _hasSample, _alarmState, _indicator);
            string key = described.Severity switch
            {
                AirAlarmSeverity.Danger => "danger",
                AirAlarmSeverity.Warning => "warn",
                _ => "good",
            };
            string status = $"<k={key}>{described.Headline.ToLowerInvariant().Replace("co2", "CO₂")}</k>";
            float temperatureC = _sampleTemperatureKelvin - 273.15f;
            sections.Add(new ExamineSection($"Air alarm: {status}."));
            sections.Add(new ExamineSection(
                $"Pressure {_samplePressureKpa:F1} kPa. " +
                $"Temp {temperatureC:F1}°C. O₂ {_sampleOxygenFraction:P1}. " +
                $"CO₂ {_sampleCarbonDioxideFraction:P2}. Plasma {_samplePlasmaFraction:P2}.",
                ExamineBand.Close));
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

        private bool IsPortInAlarmArea(AtmosPortControllerBase controller)
        {
            if (controller == null || !_hasArea)
            {
                return false;
            }

            if (!controller.TryGetComponent(out PlacedTileObject tileObject))
            {
                return false;
            }

            if (!SubSystems.TryGet(out AreaSubSystem areaSubSystem)
                || !areaSubSystem.TryGetAreaForDevice(tileObject, out AreaRecord record))
            {
                return false;
            }

            return record.Id == _areaId;
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

        private void HandleIndicatorChanged(AirAlarmIndicator _, AirAlarmIndicator __, bool asServer)
        {
            if (!asServer)
                RefreshVisuals();
        }

        private void CacheEmissiveMaterials()
        {
            _emissiveSlots ??= new EmissiveMaterialSlots();
            _emissiveSlots.Cache(_emissiveRenderers, GlowsWhenPowered);
        }

        private void RefreshVisuals()
        {
            if (_emissiveSlots == null)
                CacheEmissiveMaterials();

            // Power reaches the client before the server's next sample, so an unpowered alarm goes dark at once.
            AirAlarmIndicator indicator = IsPowered() ? _indicator : AirAlarmIndicator.Off;
            switch (indicator)
            {
                case AirAlarmIndicator.Nominal:
                    SetBlink(0f, Color.black);
                    _emissiveSlots.ApplyEmissionOverride(1f, NominalEmission);
                    break;
                case AirAlarmIndicator.Repressurizing:
                    SetBlink(TrendBlinkSeconds, RepressurizingEmission);
                    break;
                case AirAlarmIndicator.Depressurizing:
                    SetBlink(TrendBlinkSeconds, AlarmEmission);
                    break;
                case AirAlarmIndicator.Danger:
                    SetBlink(DangerFlashSeconds, AlarmEmission);
                    break;
                default:
                    SetBlink(0f, Color.black);
                    _emissiveSlots.ApplyPowered(false);
                    break;
            }
        }

        /// <summary>Blinks the face in <paramref name="emission"/> every <paramref name="seconds"/>; zero stops blinking.</summary>
        private void SetBlink(float seconds, Color emission)
        {
            if (Mathf.Approximately(_blinkSeconds, seconds) && _blinkEmission == emission)
                return;

            CancelInvoke(nameof(ToggleBlink));
            _blinkSeconds = seconds;
            _blinkEmission = emission;
            if (seconds <= 0f)
                return;

            _blinkLit = false;
            InvokeRepeating(nameof(ToggleBlink), 0f, seconds);
        }

        private void ToggleBlink()
        {
            _blinkLit = !_blinkLit;
            _emissiveSlots.ApplyEmissionOverride(1f, _blinkLit ? _blinkEmission : Color.black);
        }

        private bool IsPowered() => AtmosPortPower.IsPowered(_powerConsumer);

        /// <summary>The face of the alarm: materials authored with a glow, not the casing's black emission.</summary>
        public static bool GlowsWhenPowered(Material material) =>
            material.HasProperty(EmissionColorId) && material.GetColor(EmissionColorId).maxColorComponent > 0f;
    }
}
