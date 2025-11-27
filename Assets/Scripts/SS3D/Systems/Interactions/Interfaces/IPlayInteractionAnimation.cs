using SS3D.Systems.Interactions;

namespace SS3D.Systems.Animations
{
    /// <summary>
    /// Interface used to play an animation based on an interaction type.
    /// </summary>
    public interface IPlayInteractionAnimation
    {
        public void PlayAnimation(InteractionType interactionType);

        public void StopAnimation();
    }
}
