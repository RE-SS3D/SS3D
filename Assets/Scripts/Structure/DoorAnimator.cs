using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class DoorAnimator : NetworkBehaviour
{
    private const float DOOR_WAIT_CLOSE_TIME = 1.0f;
    private const float OPEN_SPEED = 1.5f;

    [SerializeField] private Transform leftPanel;
    [SerializeField] private Transform rightPanel;

    [SerializeField] private AudioSource openSfx;
    [SerializeField] private AudioSource closeSfx;

    [SerializeField] private int doorTriggerLayers = -1;

    // TODO: Use real animations and remove the open/closed left/right
    [SerializeField] private Vector3 openLeft;
    [SerializeField] private Vector3 openRight;

    private Vector3 closedLeft;
    private Vector3 closedRight;

    // Server Only
    [SyncVar(hook = "SetDoorState")]
    private bool openState;

    // Server Only
    private int playersInTrigger;
    private Coroutine closeTimer;

    public void Awake()
    {
        if (doorTriggerLayers != -1)
            doorTriggerLayers = LayerMask.NameToLayer("Player");

        closedLeft = leftPanel.localPosition;
        closedRight = rightPanel.localPosition;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        if (isClientOnly)
            SetDoorState(openState);
    }
    
    public void OnTriggerEnter(Collider other)
    {
        if ((other.gameObject.layer & doorTriggerLayers) == 0 || !isServer) return;

        if (playersInTrigger == 0)
        {
            if (closeTimer != null) StopCoroutine(closeTimer);
            openState = true;
        }

        playersInTrigger += 1;
    }

    public void OnTriggerExit(Collider other)
    {
        if ((other.gameObject.layer & doorTriggerLayers) == 0 || !isServer) return;

        if (playersInTrigger == 1)
        {
            // Start the close timer (which may be stopped).
            closeTimer = StartCoroutine(RunCloseEventually());
        }

        playersInTrigger = Math.Max(playersInTrigger - 1, 0);
    }

    private void SetDoorState(bool open)
    {
        if (openState == open)
            return;
        openState = open;

        if (isClient)
        {
            (open ? openSfx : closeSfx).Play();
            StartCoroutine(RunDoorAnim(open));
        }
    }

    /*
     * Runs the animation.
     * TODO: Replace this with a real animation.
     */
    private IEnumerator RunDoorAnim(bool open)
    {
        float animTime = 0.0f;

        while (animTime < 1f) {
            animTime += Time.deltaTime * OPEN_SPEED;

            float lerp = open ? animTime : 1.0f - animTime;

            leftPanel.localPosition = Vector3.Lerp(closedLeft, openLeft, lerp);
            rightPanel.localPosition = Vector3.Lerp(closedRight, openRight, lerp);

            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(1f);
    }

    private IEnumerator RunCloseEventually()
    {
        yield return new WaitForSeconds(DOOR_WAIT_CLOSE_TIME);
        openState = false;
    }
}