using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Atmospherics.AtmosRework.Machinery;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    public abstract class TrinaryAtmosDevice : BasicAtmosDevice, IInteractionTarget
    {
        [SerializeField]
        private bool _sideOnRight;

        public TileLayer PipeLayer { get; private set; } = TileLayer.PipeRight;

        protected Vector3 FrontPipePosition { get; private set; }

        protected Vector3 BackPipePosition { get; private set; }

        protected Vector3 SidePipePosition { get; private set; }

        protected IAtmosPipe FrontPipe { get; private set; }

        protected IAtmosPipe BackPipe { get; private set; }

        protected IAtmosPipe SidePipe { get; private set; }

        protected bool AllPipesConnected => FrontPipe != null && BackPipe != null && SidePipe != null;

        public bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point) => this.GetInteractionPoint(source, out point);

        public override void StepAtmos(float dt)
        {
            FrontPipePosition = Transform.position + Transform.forward;
            BackPipePosition = Transform.position - Transform.forward;
            SidePipePosition = _sideOnRight ? Transform.position + Transform.right : Transform.position - Transform.right;

            IAtmosPipe frontPipe;
            IAtmosPipe backPipe;
            IAtmosPipe sidePipe;
            Subsystems.Get<PipeSubSystem>().TryGetAtmosPipe(FrontPipePosition, PipeLayer, out frontPipe);
            Subsystems.Get<PipeSubSystem>().TryGetAtmosPipe(BackPipePosition, PipeLayer, out backPipe);
            Subsystems.Get<PipeSubSystem>().TryGetAtmosPipe(SidePipePosition, PipeLayer, out sidePipe);
            FrontPipe = frontPipe;
            BackPipe = backPipe;
            SidePipe = sidePipe;
        }

        public abstract IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent);
    }
}
