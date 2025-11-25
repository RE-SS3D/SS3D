using SS3D.Content.Systems.Interactions;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    public class ValveAtmosObject : NetworkActor, IInteractionTarget, IAtmosPipe
    {
        public enum ValveType
        {
            Manual = 0,
            Digital = 1,
        }

        [SerializeField]
        private ValveType _valveType;

        [SerializeField]
        private bool _isEnabled = true;

        public PlacedTileObject PlacedTileObject => GetComponent<PlacedTileObject>();

        public int PipeNetIndex { get; set; }

        public AtmosObject AtmosObject { get; set; }

        public TileLayer TileLayer { get; private set; }

        public Vector2Int WorldOrigin { get; private set; }

        public bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point) => this.GetInteractionPoint(source, out point);

        public override void OnStartServer()
        {
            base.OnStartServer();

            AtmosObject = new(new(0, 0), 0.5f);
            TileLayer = PlacedTileObject.Layer;
            WorldOrigin = PlacedTileObject.WorldOrigin;

            if (Subsystems.Get<PipeSubSystem>().IsSetUp)
            {
                Subsystems.Get<PipeSubSystem>().RegisterPipe(this);
            }
            else
            {
                Subsystems.Get<PipeSubSystem>().OnSystemSetUp += () => Subsystems.Get<PipeSubSystem>().RegisterPipe(this);
            }
        }

        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[]
            {
                new SimpleInteraction
                {
                    Name = _isEnabled ? "Close valve" : "Open valve", Interact = ValveInteract, RangeCheck = true,
                },
            };
        }

        private void ValveInteract(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            SetValve(!_isEnabled);
        }

        private void SetValve(bool enable)
        {
            _isEnabled = enable;

            if (_isEnabled)
            {
                Subsystems.Get<PipeSubSystem>().RegisterPipe(this);
            }
            else
            {
                Subsystems.Get<PipeSubSystem>().RemovePipe(this);
            }
        }
    }
}
