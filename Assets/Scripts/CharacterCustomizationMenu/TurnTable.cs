using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TurnTable : MonoBehaviour, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    public float defaultRotSpeed = 1;
    public Transform turnTable = null;
    public float timeTillAutoTurn = 2;

    private float timeSinceInteraction = 0;
    private bool manualRot = false;
    private Vector2 lastFrameScreenPos;
    private float frameRot = 0;

    void Update()
    {
        if (timeSinceInteraction >= timeTillAutoTurn)
            manualRot = false;
        else if (manualRot == true)
            timeSinceInteraction += Time.deltaTime;

        if (manualRot == false)
            frameRot = defaultRotSpeed * Time.deltaTime;
        turnTable.Rotate(0, frameRot, 0);
        frameRot = 0;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        manualRot = true;
        timeSinceInteraction = 0;
        lastFrameScreenPos = eventData.pointerCurrentRaycast.screenPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        timeSinceInteraction = 0;
        frameRot = lastFrameScreenPos.x - eventData.pointerCurrentRaycast.screenPosition.x;
        lastFrameScreenPos = eventData.pointerCurrentRaycast.screenPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        frameRot = 0;
    }
}
