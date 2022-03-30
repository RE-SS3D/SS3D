using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MaterialColorer : MonoBehaviour
{
    private string state = "black";
    private Color currentColor;
    [Tooltip("A list of objects that will be recolored when the event is called.")]
    public MeshRenderer[] objectsToRecolor;
    [Tooltip("Which material slot in the Mesh Renderer should be recolored?")]
    public int materialIndex = 0;
    
    public void ChangeMaterialState(string changeStateTo)
    {
        switch (changeStateTo)
        {
            case "off":
            case "black":
            case "null":
            case "no power":
            case "neutral":
            case "default":
                currentColor = Color.black;
                break;
            case "on":
            case "green":
            case "approve":
            case "accept":
            case "positive":
            case "yes":
                currentColor = new Color(.07f, 1f, .32f);
                break;
            case "deny":
            case "red":
            case "error":
            case "negative":
            case "no":
                currentColor = new Color(1, 0.18f, .2f);
                break;
        }
        ChangeColors(currentColor);
    }

    private void ChangeColors(Color newColor)
    {
        foreach (var item in objectsToRecolor)
        {
            item.materials[materialIndex].color = newColor;
        }
    }
}
