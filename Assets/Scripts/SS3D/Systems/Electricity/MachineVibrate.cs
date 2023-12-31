using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet;
using SS3D.Core.Behaviours;
using UnityEngine;

/// <summary>
/// Little script to make stuff vibrate back and forth. Mostly useful for electrical furnitures functionning.
/// </summary>
public class MachineVibrate : Actor
{
    [SerializeField]
    private float _amplitude = 1; // How much is the amplitude of the vibration.
    [SerializeField]
    private float _frequency = 35;       // Vibrating speed.
    private Quaternion _initialRotation; // Rotation of the generator at rest.
    private Vector3 _directionOfShake;   // In which direction the generator shake.
    private bool _enable = false;
    private float _elapsedTime = 0f; // Elapsed time for the vibrating stuff.

    public bool Enable
    {
        get => _enable;
        set => SetEnable(value);
    }

    protected override void OnStart()
    {
        base.OnStart();
        // do not need to show the vibrating stuff on server
        if (InstanceFinder.IsServerOnly) return;

        AddHandle(FixedUpdateEvent.AddListener(HandleFixedUpdate));
        _initialRotation = Rotation;
        _directionOfShake = Transform.right;
    }

    private void HandleFixedUpdate(ref EventContext context, in FixedUpdateEvent updateEvent)
    {
        if (_enable)
        {
            Vibrate();
        }
    }
    
    private void Vibrate()
    {
        _elapsedTime += Time.fixedDeltaTime;
        transform.rotation = _initialRotation * Quaternion.Euler(_directionOfShake * (-_amplitude + Mathf.PingPong(_frequency * _elapsedTime, 2f * _amplitude)));
    }

    private void SetEnable(bool enable)
    {
        _enable = enable;
        if (enable)
        {
            Rotation = _initialRotation;
        }
    }
}
