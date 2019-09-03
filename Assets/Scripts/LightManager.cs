using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightManager : MonoBehaviour
{
    [SerializeField]
    float power;

    [SerializeField]
    States state = States.defaultCase;
    enum States { defaultCase, off, broken, noLight, emergency };

    Color c;

    [SerializeField]
    Light pointLight;
    [SerializeField]
    GameObject lightObject;
    [SerializeField]
    GameObject brokenLightObject;

    void Start()
    {

    }


    void Update()
    {
        pointLight.intensity = power;

        switch (state)
        {
            case States.defaultCase:
                lightObject.SetActive(true);
                brokenLightObject.SetActive(false);
                c = new Color(0.945098f, 0.945098f, 0.8f);
                pointLight.color = c;
                break;
            case States.off:
                lightObject.SetActive(true);
                pointLight.intensity = 0;
                brokenLightObject.SetActive(false);
                break;
            case States.broken:
                lightObject.SetActive(false);
                brokenLightObject.SetActive(true);
                break;
            case States.noLight:
                lightObject.SetActive(false);
                brokenLightObject.SetActive(false);
                break;
            case States.emergency:
                lightObject.SetActive(true);
                brokenLightObject.SetActive(false);
                c = new Color(1f, 0.1607843f, 0.1960784f);
                pointLight.color = c;
                break;
        }

   
    }


}
