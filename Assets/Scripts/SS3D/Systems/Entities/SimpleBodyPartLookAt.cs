using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// This class is used to find where the mouse position is and move a target to it.
    /// Will be removed eventually
    /// </summary>
    public class SimpleBodyPartLookAt : MonoBehaviour
    {
        [FormerlySerializedAs("camera")] [SerializeField] private Camera _camera;
        [FormerlySerializedAs("target")] [SerializeField] public Transform _target;
        private Vector3 _mousePos = Vector3.zero;

        [FormerlySerializedAs("rotationSpeed")]
        [Range(0.5f, 10)]
        [SerializeField] public float _rotationSpeed = 5;

        private void Start()
        {
            _target.position = transform.position;

            //_camera = CameraManager.singleton.playerCamera.GetComponent<Camera>();
        }

        public void MoveTarget()
        {
            _mousePos = Vector3.Lerp(_mousePos, GetMousePosition(false), Time.deltaTime * _rotationSpeed);
            _target.position = _mousePos;
        }

        public Vector3 GetMousePosition(bool changeYAxis)
        {
            return _mousePos;
        }
    }
}