using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DummyTransformHelper
{

    public static IEnumerator LerpTransform(Transform transform, Transform destination, float duration,
        bool lerpPosition = true, bool lerpRotation = true, bool parent = true)
    {
        Vector3 initialPosition = transform.position;
        Quaternion initialRotation = transform.rotation;
        float timer = 0.0f;

        while (timer < duration)
        {
            float t = timer / duration;
            if(lerpPosition)
                transform.position = Vector3.Lerp(initialPosition, destination.position, t);
            if(lerpRotation)
                transform.rotation = Quaternion.Lerp(initialRotation, destination.rotation, t);

            timer += Time.deltaTime;
            yield return null;
        }

        // Ensure final transform matches the target values exactly
        if(lerpPosition)
            transform.position = destination.position;
        if(lerpRotation)
            transform.rotation = destination.rotation;

        if (parent)
        {
            transform.parent = destination;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }

    public static IEnumerator OrientTransformTowardTarget(Transform transform, Transform target, float duration,
        bool ignoreX = false, bool ignoreY = false, bool ignoreZ = false)
    {
        float elapsedTime = 0f;
        
        Vector3 directionFromTransformToTarget = target.position - transform.position;
        
        if(ignoreX)
            directionFromTransformToTarget.x = 0f;
        if(ignoreY)
            directionFromTransformToTarget.y = 0f;
        if(ignoreZ)
            directionFromTransformToTarget.z = 0f;
        
        Quaternion rotation = Quaternion.LookRotation(directionFromTransformToTarget);

        while (elapsedTime < duration)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, rotation, elapsedTime/duration);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
}
