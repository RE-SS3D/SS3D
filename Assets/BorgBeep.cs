using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BorgBeep : MonoBehaviour

   

{

    [SerializeField] bool on = false;

    [SerializeField] Light l1;
    [SerializeField] Light l2;

    [SerializeField] MeshRenderer beepLight;

    [SerializeField] float intensity;

    [SerializeField] Material lOn;
    [SerializeField] Material lOff;

    MaterialChanger mc = new MaterialChanger();
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("c"))
        {
            UseSelf();
        }
    }

    public void UseSelf()
    {
        if (!on)
        {
            mc.ChangeMaterial(beepLight, lOn);
            l1.intensity = intensity;
            l2.intensity = intensity;
            on = true;

        }
        else
        {
            mc.ChangeMaterial(beepLight, lOff);
            l1.intensity = 0f;
            l2.intensity = 0f;
            on = false;

        }
    }
}
