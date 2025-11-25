using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using SS3D.Content.Systems.Interactions;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Atmospherics.AtmosRework.Machinery;
using SS3D.Systems.Tile;
using Unity.Mathematics;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    public class UnaryVentAtmos : BasicAtmosDevice, IInteractionTarget
    {
        private enum PressureEqualizingMode
        {
            Internal = 0,
            External = 1,
        }

        private enum OperatingMode
        {
            Pump = 0,
            Suck = 1,
        }

        private const float RotationSpeedSucking = 1f;

        private const float RotationSpeedPumping = -1f;

        private bool _deviceActive = true;

        private TileLayer _pipeLayer = TileLayer.PipeLeft;

        private float _targetPressure = 101.3f;

        private PressureEqualizingMode _pressureMode = PressureEqualizingMode.External;

        private OperatingMode _operatingMode = OperatingMode.Pump;

        [SerializeField]
        private Transform _fans;

        private TweenerCore<Quaternion, Vector3, QuaternionOptions> _rotationTween;

        public bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point) => this.GetInteractionPoint(source, out point);

        public override void OnStartServer()
        {
            base.OnStartServer();
            Animate();
        }

        public override void StepAtmos(float dt)
        {
            if (!_deviceActive)
            {
                return;
            }

            if (!Subsystems.Get<PipeSubSystem>().TryGetAtmosPipe(transform.position, _pipeLayer, out IAtmosPipe pipe))
            {
                return;
            }

            AtmosObject atmosEnv = Subsystems.Get<AtmosEnvironmentSubSystem>().GetAtmosContainer(transform.position).AtmosObject;
            AtmosObject atmosPipe = pipe.AtmosObject;

            if ((_pressureMode == PressureEqualizingMode.External && atmosEnv.Pressure > _targetPressure) || (_pressureMode == PressureEqualizingMode.Internal && atmosPipe.Pressure > _targetPressure))
            {
                return;
            }

            AtmosObject atmosToTransferTo = _operatingMode == OperatingMode.Pump ? atmosEnv : atmosPipe;
            AtmosObject atmosToTransferFrom = _operatingMode == OperatingMode.Suck ? atmosEnv : atmosPipe;

            float4 toTransfer = AtmosCalculator.MolesToTransfer(atmosToTransferTo, atmosToTransferFrom, true, dt, 0f, 0f);

            if (!math.any(toTransfer > 0f))
            {
                return;
            }

            if (_operatingMode == OperatingMode.Pump)
            {
                Subsystems.Get<AtmosEnvironmentSubSystem>().AddGasses(transform.position, toTransfer);
                Subsystems.Get<PipeSubSystem>().RemoveCoreGasses(transform.position, toTransfer, _pipeLayer);
            }
            else
            {
                Subsystems.Get<AtmosEnvironmentSubSystem>().RemoveGasses(transform.position, toTransfer);
                Subsystems.Get<PipeSubSystem>().AddCoreGasses(transform.position, toTransfer, _pipeLayer);
            }
        }

        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[]
            {
                new SimpleInteraction
                {
                    Name = _deviceActive ? "Stop vent" : "Start vent", Interact = ActiveInteract, RangeCheck = true,
                },
                new SimpleInteraction
                {
                    Name = _pressureMode == PressureEqualizingMode.Internal ? "External mode" : "Internal mode", Interact = PressureModeInteract, RangeCheck = true,
                },
                new SimpleInteraction
                {
                    Name = _operatingMode == OperatingMode.Suck ? "Pump mode" : "Suck mode", Interact = OperatingModeInteract, RangeCheck = true,
                },
            };
        }

        private void OperatingModeInteract(InteractionEvent arg1, InteractionReference arg2)
        {
            _operatingMode = _operatingMode switch
            {
                OperatingMode.Pump => OperatingMode.Suck,
                OperatingMode.Suck => OperatingMode.Pump,
                _ => _operatingMode,
            };

            Animate();
        }

        private void ActiveInteract(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            _deviceActive = !_deviceActive;
            Animate();
        }

        private void PressureModeInteract(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            _pressureMode = _pressureMode switch
            {
                PressureEqualizingMode.Internal => PressureEqualizingMode.External,
                PressureEqualizingMode.External => PressureEqualizingMode.Internal,
                _ => _pressureMode,
            };
        }

        private void Animate()
        {
            if (_rotationTween != null && _rotationTween.IsActive())
            {
                _rotationTween.Kill(); // Stops the tween
                _rotationTween = null; // Reset the reference
            }

            if (_deviceActive)
            {
                float speed = _operatingMode == OperatingMode.Pump ? RotationSpeedPumping : RotationSpeedSucking;

                _rotationTween = _fans.DORotate(Vector3.up * (speed * 360), 1f, RotateMode.WorldAxisAdd).
                    SetEase(Ease.Linear) // Ensures constant speed
                    .
                    SetLoops(-1, LoopType.Incremental);
            }
        }
    }
}
