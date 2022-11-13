using DG.Tweening;
using SS3D.Core.Behaviours;
using UnityEngine;

namespace SS3D.Networking
{
    public class ScaleIn : NetworkedSpessBehaviour
    {
        public Transform ObjectToScale;

        private const float ScaleDuration = .85f; 

        protected override void OnStart()
        {
            base.OnStart();

            ObjectToScale.DOScale(0, 0);
            ObjectToScale.DOScale(1, ScaleDuration).SetEase(Ease.OutBounce);
        }
    }
}