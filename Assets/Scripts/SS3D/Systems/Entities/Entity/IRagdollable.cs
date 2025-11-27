using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRagdollable
{
    public void Knockdown(float time);
    
    public void Recover();

    public void AddForceToAllRagdollParts(Vector3 vector3);

    public bool IsRagdolled();
}
