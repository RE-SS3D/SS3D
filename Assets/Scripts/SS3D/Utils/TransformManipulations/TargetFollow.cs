using UnityEngine;

/// <summary>
/// Utility component allowing a gameobject to reproduce another game object's rotation and position without parenting.
/// </summary>
public class TargetFollow : MonoBehaviour
{

    public Transform Followed { get; private set; }

    private bool _reproduceRotation;

    private float _timeToReachRotation;

    private float _timing;

    private Vector3 _initialPosition;

    private bool _update;

    public void Follow(Transform followed, bool reproduceRotation, float timeToReachRotation = 0, bool update = true)
    {
        _initialPosition = followed.position;
        Followed = followed;
        _reproduceRotation = reproduceRotation;
        _timeToReachRotation = timeToReachRotation;
        _timing = 0;
        _update = update;
        UpdateFollow();
    }

    private void LateUpdate()
    {
        if (Followed is null) 
        {
            return;
        }

        UpdateFollow();
    }

    private void UpdateFollow()
    {
        if (_update)
        {
            transform.position = Followed.position;
        }
        else
        {
            transform.position = _initialPosition;
        }
        

        if (!_reproduceRotation) { return; }

        transform.rotation = _timing >= _timeToReachRotation ? Followed.rotation : Quaternion.Slerp(transform.rotation, Followed.rotation, _timing/_timeToReachRotation);
        _timing += Time.deltaTime;
    }
}
