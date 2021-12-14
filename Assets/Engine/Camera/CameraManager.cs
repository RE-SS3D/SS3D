using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This handles all the cameras in the game
public class CameraManager : MonoBehaviour
{
    public static CameraManager singleton { get; private set; }

    public Camera playerCamera;

    private void Awake()
    {
        if (singleton != null) Destroy(gameObject);
        singleton = this;
    }
}
