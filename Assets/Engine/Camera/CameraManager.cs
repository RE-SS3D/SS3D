using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager singleton { get; private set; }

    public Camera lobbyCamera;
    public Camera playerCamera;
    public Camera examineCamera;

    private void Awake()
    {
        if (singleton != null) Destroy(gameObject);
        singleton = this;
    }
}
