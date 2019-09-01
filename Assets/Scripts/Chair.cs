using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chair : InteractableObject
{
    public override void LeftMouseHeldOneSecond()
    {
        //Ignores the base class method
        print("Can't Kill me!");
    }

    public override void LeftMousePress(Vector3 newPos)
    {
        base.LeftMousePress(newPos);
    }

    public override void RightMousePress(Quaternion newRot)
    {
        base.RightMousePress(newRot);
    }
}

