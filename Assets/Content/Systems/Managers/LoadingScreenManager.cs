using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using SS3D;
using SS3D.Engine.Tiles;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class LoadingScreenManager : MonoBehaviour
{
    [SerializeField] private Sprite[] wallpapers;
    [SerializeField] private Animator animator;

    [SerializeField]
    private Image image;

    private void Start()
    {
        SceneLoaderManager.mapLoaded += delegate { Toggle(true); };
        TileManager.tileManagerLoaded += delegate { Toggle(false); };
    }

    public void Toggle(bool state)
    {
        if (!animator.enabled) animator.enabled = true;
        animator.SetBool("Toggle", state);
    }
    private void OnEnable()
    {
        image.sprite = wallpapers[Random.Range(0, wallpapers.Length - 1)];
    }
}
