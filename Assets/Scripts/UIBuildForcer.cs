using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//This script exists solely to fix the layout building of the character creation menu, as soon as someone fixes it or redo it it should be removed
public class UIBuildForcer : MonoBehaviour
{
    public void Update()
    {
        LayoutRebuilder.MarkLayoutForRebuild(this.GetComponent<RectTransform>());//ForceRebuildLayoutImmediate(this.GetComponent<RectTransform>());
    }
}
