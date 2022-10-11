using System;
using System.Collections;
using System.Collections.Generic;
using Coimbra;
using UnityEngine;

public class DestroyIfNotDevelopmentBuild : MonoBehaviour
{
    private void Awake()
    {
        if (Debug.isDebugBuild)
        {
            gameObject.Destroy();
        }
    }
}
