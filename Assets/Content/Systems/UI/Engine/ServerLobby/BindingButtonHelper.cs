using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;

public class BindingButtonHelper : MonoBehaviour
{
    private bool isListening = false;

    // Update is called once per frame
    void Update()
    {
        if (isListening && Input.inputString != "")
        {
            KeybindingManager.keyToEntry[(KeyCode)Enum.Parse(typeof(KeyCode), gameObject.transform.Find("Text (TMP)").GetComponent<TMPro.TMP_Text>().text, true)].key = (KeyCode)Enum.Parse(typeof(KeyCode), Input.inputString, true);
            gameObject.transform.Find("Text (TMP)").GetComponent<TMPro.TMP_Text>().text = Input.inputString;
            UnityEngine.Debug.Log(Input.inputString);
            isListening = false;
        }
    }

    public void onClicked()
    {
        isListening = true;
    }
}
