using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using SS3D.Content.Systems.Interactions;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Atmospherics.AtmosRework.Machinery;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    public class ScrubberAtmos : BasicAtmosDevice, IInteractionTarget
    {
        private enum OperatingMode
        {
            Scrubbing = 0,
            Siphoning = 1,
        }

        private enum Range
        {
            Normal = 0,
            Extended = 1,
        }

        private const float RotationSpeedScrubbing = 1f;

        private const float RotationSpeedSiphoning = 1.5f;

        private readonly float _molesRemovedPerSecond = 500f;

        private OperatingMode _mode = OperatingMode.Scrubbing;

        private Range _range = Range.Normal;

        private bool4 _filterCoreGasses;

        private TileLayer _pipeLayer = TileLayer.PipeLeft;

        private bool _deviceActive = true;

        private Vector3[] _atmosNeighboursPositions;

        [SerializeField]
        private Transform _fans;

        private TweenerCore<Quaternion, Vector3, QuaternionOptions> _rotationTween;

        public bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point) => this.GetInteractionPoint(source, out point);

        public override void OnStartServer()
        {
            base.OnStartServer();
            Vector3 position = transform.position;
            _atmosNeighboursPositions = new Vector3[]
            {
                position,
                position + Vector3.forward,
                position + Vector3.back,
                position + Vector3.right,
                position + Vector3.left,
            };

            Animate();
        }

        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[]
            {
                new SimpleInteraction
                {
                    Name = _deviceActive ? "Stop scrubbber" : "Start scrubber", Interact = ActiveInteract, RangeCheck = true,
                },
                new SimpleInteraction
                {
                    Name = _mode == OperatingMode.Scrubbing ? "Siphon mode" : "Scrubbing mode", Interact = ModeInteract, RangeCheck = true,
                },
            };
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

            if (pipe.AtmosObject.Pressure > 5000)
            {
                return;
            }

            int numOfTiles = _range switch
            {
                Range.Normal => 1,
                Range.Extended => 5,
                _ => 0,
            };

            // We loop 1 or 5 times based on the range setting
            for (int i = 0; i < numOfTiles; i++)
            {
                AtmosObject atmos = Subsystems.Get<AtmosEnvironmentSubSystem>().GetAtmosContainer(_atmosNeighboursPositions[i]).AtmosObject;
                float4 toSiphon = 0;

                if (_mode == OperatingMode.Siphoning)
                {
                    toSiphon = atmos.CoreGassesProportions * math.min(atmos.TotalMoles, _molesRemovedPerSecond * dt);
                }

                // If scrubbing, remove only filtered gas
                else
                {
                    float4 filteredGasses = atmos.CoreGasses * (int4)_filterCoreGasses;
                    toSiphon = (filteredGasses / math.csum(filteredGasses)) * math.min(math.csum(filteredGasses), _molesRemovedPerSecond * dt);
                    toSiphon = math.any(math.isnan(toSiphon)) ? 0 : toSiphon;
                }

                if (math.all(toSiphon == 0))
                {
                    continue;
                }

                Subsystems.Get<PipeSubSystem>().AddCoreGasses(pipe.PlacedTileObject.gameObject.transform.position, toSiphon, _pipeLayer);
                Subsystems.Get<AtmosEnvironmentSubSystem>().RemoveGasses(_atmosNeighboursPositions[i], toSiphon);
            }
        }

        private void ActiveInteract(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            _deviceActive = !_deviceActive;
            Animate();
        }

        private void ModeInteract(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            _mode = _mode switch
            {
                OperatingMode.Scrubbing => OperatingMode.Siphoning,
                OperatingMode.Siphoning => OperatingMode.Scrubbing,
                _ => _mode,
            };
            Animate();
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
                float speed = _mode == OperatingMode.Scrubbing ? RotationSpeedScrubbing : RotationSpeedSiphoning;
                _rotationTween = _fans.DORotate(Vector3.up * (speed * 360), 1f, RotateMode.WorldAxisAdd)
                    .SetEase(Ease.Linear) // Ensures constant speed
                    .SetLoops(-1, LoopType.Incremental);
            }
        }
    }
}
