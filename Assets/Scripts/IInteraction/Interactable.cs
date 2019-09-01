using UnityEngine;
using UnityEngine.Events;

public abstract class InteractableObject : MonoBehaviour
{
    public virtual void LeftMousePress(Vector3 newPos)
    {
        transform.position = newPos;
    }

    public virtual void RightMousePress(Quaternion newRot)
    {
        transform.rotation = newRot;
    }

    public virtual void LeftMouseHeldOneSecond()
    {
        Destroy(gameObject);
    }
    //..ect
}

