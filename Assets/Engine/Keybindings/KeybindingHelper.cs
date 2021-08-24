using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeybindingHelper
{
    public static bool GetButtonDown(string name)
    {
        return Input.GetKeyDown(KeybindingManager.nameToEntry[name].key);
    }

    public static bool GetButtonUp(string name)
    {
        return Input.GetKeyUp(KeybindingManager.nameToEntry[name].key);
    }

    public static bool GetButton(string name)
    {
        return Input.GetKey(KeybindingManager.nameToEntry[name].key);
    }
}
