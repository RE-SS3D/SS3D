using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyboxController : MonoBehaviour
{
    private Skybox current;
    
    [Range(0.005f, 0.02f)]
    public float rotationRate = 0.02f;

    private void Awake()
    {
        current = GetComponent<Skybox>();

        Material clone = new Material(current.material);
        current.material = clone;
    }

    void FixedUpdate()
    {
        current.material.SetFloat(("_Rotation"), current.material.GetFloat("_Rotation") + rotationRate);
    }
}
