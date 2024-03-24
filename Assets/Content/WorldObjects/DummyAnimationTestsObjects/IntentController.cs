using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntentController : MonoBehaviour
{
    public Intent intent;

    public event EventHandler<Intent> OnIntentChange;
    
    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            intent = intent == Intent.Throw ? Intent.Def : Intent.Throw; 
            OnIntentChange?.Invoke(this, intent);
            return;
        }
        
        if (!Input.GetKeyDown(KeyCode.I))
            return;

        intent = intent == Intent.Def ? Intent.Harm : Intent.Def; 
        
        OnIntentChange?.Invoke(this, intent);
        
        Debug.Log($"Selected intent is {intent}");
    }
}
