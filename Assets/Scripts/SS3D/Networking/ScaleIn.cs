using DG.Tweening;
using SS3D.Core.Behaviours;

namespace SS3D.Networking
{
    public class ScaleIn : NetworkedSpessBehaviour
    {
        private const float ScaleDuration = .85f; 

        protected override void OnStart()
        {
            base.OnStart();

            TransformCache.DOScale(0, 0);
            TransformCache.DOScale(1, ScaleDuration).SetEase(Ease.OutBounce);
        }
    }
}
