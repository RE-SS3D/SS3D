using DG.Tweening;
using System;
using UnityEngine;

namespace SS3D.Utils
{
    /// <summary>
    /// Fades a CanvasGroup alpha property by seconds
    /// </summary>
    public sealed class UiFade : MonoBehaviour
    {
        [SerializeField] private GameObject _root;

        [Header("Components")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Settings")]
        [SerializeField] private float _transitionDuration;
        [SerializeField] private State _intendedState;
        [SerializeField] private bool _fadeOnStart;

        private Sequence _sequence;
        
        private bool _currentState;

        private enum State
        {
            On = 1,
            Off = 0
        }

        private void Start()
        {
            Setup();
        }

        private void Setup()
        {
            if (_fadeOnStart)
            {
                ProcessFade();
            }

            _canvasGroup.alpha = _intendedState == State.On ? 0 : 1;
        }

        public void SetFade(bool state)
        {
            _intendedState = state ? State.On : State.Off;
            ProcessFade();
        }

        public void SetTransitionDuration(float duration)
        {
            _transitionDuration = duration;
        }

        public void ProcessFade(Action callback = null)
        {
            _sequence?.Kill();
            _sequence = DOTween.Sequence();
  
            _root.SetActive(true);      

            bool fadeOut = _intendedState == State.On;
            _currentState = fadeOut;
            _canvasGroup.alpha = fadeOut ? 0 : 1;

            if (callback != null)
            {
                _canvasGroup.DOFade((int)_intendedState, _transitionDuration).OnComplete(callback.Invoke).SetEase(Ease.InCubic);
            }
            else
            {
                _canvasGroup.DOFade((int)_intendedState, _transitionDuration).SetEase(Ease.InCubic);
            }

            _currentState = !_currentState;
        }
    }
}
