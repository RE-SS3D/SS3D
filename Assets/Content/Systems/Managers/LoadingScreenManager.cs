using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class LoadingScreenManager : MonoBehaviour
{
    [SerializeField] private Sprite[] wallpapers;

    [SerializeField]
    private Image image;
    
    private void OnEnable()
    {
        image.sprite = wallpapers[Random.Range(0, wallpapers.Length - 1)];
    }
}
