using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Screens;
using UnityEngine;

namespace SS3D.Systems.Entities
{
    public class GhostController : NetworkedSpessBehaviour
    {
        [SerializeField] private Entity _entity;

        [Header("Movement Settings")]
        [SerializeField] private float _movementSpeed;

        [SerializeField] private CharacterController _characterController;
        [SerializeField] private Transform _movementTarget;
        
        private Vector2 _input;
        private SpessBehaviour _camera;
        private Vector3 _targetMovement;
        private Vector3 _absoluteMovement;
        private Vector2 _smoothedInput;

        protected override void OnStart()
        {
            base.OnStart();

            _camera = GameSystems.Get<PlayerCameraSystem>().Camera.GetComponent<SpessBehaviour>();
        }

        protected override void HandleUpdate(in float deltaTime)
        {
            base.HandleUpdate(in deltaTime);
        
            if (!IsOwner)
            {
                return;
            }
            
            ProcessCharacterMovement();
        }
        
        /// <summary>
        /// Executes the movement code and updates the IK targets
        /// </summary>
        private void ProcessCharacterMovement()
        {
            ProcessPlayerInput();

            if (_input.magnitude != 0)
            {
                MoveMovementTarget(_input);
                RotatePlayerToMovement();
                MovePlayer();
            }
            else
            { 
                MovePlayer(); 
                MoveMovementTarget(Vector2.zero, 5);   
            }
        }
        
        /// <summary>
        /// Moves the movement targets with the given input
        /// </summary>
        /// <param name="movementInput"></param>
        private void MoveMovementTarget(Vector2 movementInput, float multiplier = 1)
        {
            //makes the movement align to the camera view
            Vector3 newTargetMovement =
                movementInput.y * Vector3.Cross(_camera.Right, Vector3.up).normalized +
                movementInput.x * Vector3.Cross(Vector3.up, _camera.Forward).normalized;
            
            // smoothly changes the target movement
            _targetMovement = Vector3.Lerp(_targetMovement, newTargetMovement, Time.deltaTime * (1 * multiplier));
            
            Vector3 resultingMovement = _targetMovement + transform.position;
            _absoluteMovement = resultingMovement;
        
            _movementTarget.position = _absoluteMovement;
        }
        
        /// <summary>
        /// Rotates the player to the target movement
        /// </summary>
        private void RotatePlayerToMovement()
        {
            transform.rotation = 
                Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_targetMovement), Time.deltaTime * 1);
        }
        
        /// <summary>
        /// Moves the player to the target movement
        /// </summary>
        private void MovePlayer()
        {
            _characterController.Move(_targetMovement * ((_movementSpeed) * Time.deltaTime));
        }
        
        /// <summary>
        /// Process the player movement input, smoothing it 
        /// </summary>
        /// <returns></returns>
        private void ProcessPlayerInput()
        {
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");
        
            _input = new Vector2(x, y);
        
            _smoothedInput = Vector2.Lerp(_smoothedInput, _input, Time.deltaTime * 1);
        }
    }
}
