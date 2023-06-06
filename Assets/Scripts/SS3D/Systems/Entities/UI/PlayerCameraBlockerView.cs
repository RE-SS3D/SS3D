using Coimbra.Services.Events;
using DG.Tweening;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Events;
using UnityEngine;

namespace SS3D.Systems.Entities.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PlayerCameraBlockerView : View
    {
        [SerializeField]
        private CanvasGroup _canvasGroup;

        private Sequence _alphaSequence;

        private const float FadeDuration = 2;

        protected override void OnAwake()
        {
            base.OnAwake();

            Setup();

            AddEventListeners();

            SetBlock(true);
        }

        private void AddEventListeners()
        {
            LocalPlayerObjectChanged.AddListener(HandleLocalPlayerObjectChanged);
        }

        private void HandleLocalPlayerObjectChanged(ref EventContext context, in LocalPlayerObjectChanged e)
        {
            SetBlock(!e.PlayerHasObject);
        }

        private void SetBlock(bool blockCameraVision)
        {
            _alphaSequence?.Kill();
            _alphaSequence = DOTween.Sequence();

            if (blockCameraVision)
            {
                _canvasGroup.blocksRaycasts = false;
                _alphaSequence.Append(_canvasGroup.DOFade(1, FadeDuration));
            }
            else
            {
                _alphaSequence.Append(_canvasGroup.DOFade(0, FadeDuration)).OnComplete(() => {   _canvasGroup.blocksRaycasts = true;});
            }
        }

        private void Setup()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        }
    }
}
