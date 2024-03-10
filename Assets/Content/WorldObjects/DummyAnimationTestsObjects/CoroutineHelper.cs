using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CoroutineHelper
{
    

    public static IEnumerator ModifyValueOverTime(Action<float> value,
        float startValue, float endValue, float time)
    {
        float elapsedTime = 0f;

        while (elapsedTime < time)
        {
            value(startValue + (endValue-startValue)*(elapsedTime/time));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the value reaches the target value exactly
        value(endValue);
    }
}
