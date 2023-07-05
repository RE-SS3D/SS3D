using DG.Tweening;
using SS3D.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace External.RuntimeInspector
{
    public class RuntimeEditorView : MonoBehaviour
    {
        [SerializeField] private RectTransform _runtimeInspector;

        [SerializeField] private RectTransform _runtimeHierarchy;
        [SerializeField] private float moveDuration = 0.5f;

        private float _hierarchyWidth;
        private float _inspectorWidth;
        // Used for opening/closing
        private bool _isShowed = false;
        private int _targetPoint;
        // Start is called before the first frame update
        void Start()
        {
            Subsystems.Get<SS3D.Systems.Inputs.InputSystem>().Inputs.Other.ToggleRuntimeEditor.performed += HandleToggle;
            _hierarchyWidth = _runtimeHierarchy.sizeDelta.x;
            _inspectorWidth = _runtimeInspector.sizeDelta.x;
        }

        private void HandleToggle(InputAction.CallbackContext context)
        {
            _isShowed = !_isShowed;
        }

        // Update is called once per frame
        void Update()
        {
            if (!_isShowed)
            {
                _runtimeHierarchy.DOAnchorPosX(-_hierarchyWidth, moveDuration);
                _runtimeInspector.DOAnchorPosX(_inspectorWidth, moveDuration);
            }
            else
            {
                _runtimeHierarchy.DOAnchorPosX(0, moveDuration);
                _runtimeInspector.DOAnchorPosX(0, moveDuration);
            }
        }
    }
}
