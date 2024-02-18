using Coimbra.Services.Events;
using DG.Tweening;
using SS3D.Application.Events;
using SS3D.Core.Behaviours;

namespace SS3D.Tweening
{
    /// <summary>
    /// Initializes DOTween after the application started initializing
    /// </summary>
    public class TweenInitializationTrigger : Actor
    {
        protected override void OnAwake()
        {
            base.OnAwake();

            ApplicationInitializing.AddListener(HandleApplicationInitializing);
        }

        private void HandleApplicationInitializing(ref EventContext context, in ApplicationInitializing e)
        {
            DOTween.Init();
        }
    }
}