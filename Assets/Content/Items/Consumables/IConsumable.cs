using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IConsumable
{
        void ConsumeAction(GameObject origin, GameObject target = null);
}
