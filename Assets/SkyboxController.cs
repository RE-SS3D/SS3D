using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyboxController : MonoBehaviour
{
    private Skybox current;
    
    [Range(0.005f, 0.02f)]
    public float rotationRate = 0.02f;

    private void Start()
    {
        current = GetComponent<Skybox>();
    }

    void FixedUpdate()
    {
        current.material.SetFloat(("_Rotation"), current.material.GetFloat("_Rotation") + rotationRate);
    }
}
