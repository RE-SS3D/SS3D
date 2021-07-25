using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class BlinkingLight : MonoBehaviour
{
    enum LightMode
    {
        StayOn,
        Blinking,
        Off
    }
    
    private MeshRenderer lightMesh;
    private LightMode lightMode = LightMode.Off;
    private IEnumerator blinkCoroutine;
    
    [Header("Materials")]
    [SerializeField] private Material nonEmissiveMaterial;
    [SerializeField] private Material emissiveMaterial;

    [Header("Blink Settings")]
    [Tooltip("Seconds between blinks.")]
    [SerializeField] private float blinkPeriod = 1f;
    [Tooltip("How long should the light stay on when blinking. Must be less than blink period.")]
    [SerializeField] private float blinkLength = .5f;
    
    void Start() 
    {
        lightMesh = GetComponent<MeshRenderer>();
        TurnLightOff();
        blinkCoroutine = StartBlinking();
    }
    
    public void TurnLightOff() 
    {
        if (lightMode != LightMode.Off)
        {
            StopCoroutine(blinkCoroutine);
            lightMesh.material = nonEmissiveMaterial;
            lightMode = LightMode.Off;
        }
    }
    
    public void MakeLightStayOn() {
        if (lightMode != LightMode.StayOn)
        {
            StopCoroutine(blinkCoroutine);
            lightMode = LightMode.StayOn;
            lightMesh.material = emissiveMaterial;
        }
    }
    
    public void MakeLightBlink() 
    {
        // guard clause to prevent the coroutine from being created multiple times
        if (lightMode != LightMode.Blinking)
        {
            lightMode = LightMode.Blinking;
            StartCoroutine(blinkCoroutine);
        }
    }
    
    IEnumerator StartBlinking()
    {
        while (lightMode == LightMode.Blinking)
        {
            lightMesh.material = emissiveMaterial;
            yield return new WaitForSeconds(blinkLength);
            lightMesh.material = nonEmissiveMaterial;
            yield return new WaitForSeconds(blinkPeriod - blinkLength);
        }
    }
}
