using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntentController : MonoBehaviour
{
    public Intent intent;
    
    // Update is called once per frame
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.I))
            return;

        intent = intent == Intent.Def ? Intent.Harm : Intent.Def; 
        
        Debug.Log($"Selected intent is {intent}");
    }
}
