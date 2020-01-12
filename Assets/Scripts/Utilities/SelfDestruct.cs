using UnityEngine;

/// <summary>
/// Helper MonoBehaviour to destroy a GameObject after a set timer.
/// </summary>
public class SelfDestruct : MonoBehaviour
{
    [SerializeField] private float time;

    private void Start()
    {
        Destroy(gameObject, time);
    }
}
