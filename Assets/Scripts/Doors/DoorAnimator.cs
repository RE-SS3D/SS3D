using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class DoorAnimator : NetworkBehaviour
{
    private List<GameObject> insidePlayers = new List<GameObject>();
    [SerializeField] private float openSpeed = 3f;
    [SerializeField] private Transform leftPanel;
    [SerializeField] private Transform rightPanel;

    private Vector3 closedLeft;
    private Vector3 closedRight;

    [SerializeField] private Vector3 openLeft;
    [SerializeField] private Vector3 openRight;
    private float lerpTime;
    private bool isOpening;
    private bool isClosing;
    private bool isOpened;

    private int playerLayer;
    private int playersInTrigger;

    void Awake()
    {
        closedLeft = leftPanel.localPosition;
        closedRight = rightPanel.localPosition;
        playerLayer = LayerMask.NameToLayer("Player");
    }

    public void OnTriggerEnter(Collider other)
    {
        
        if (other.gameObject.layer != playerLayer) return;
        
        if (!insidePlayers.Contains(other.gameObject))
        {
            insidePlayers.Add(other.gameObject);
            Debug.Log($"Player added to the inside count {insidePlayers.Count}");
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer != playerLayer) return;
        
        if (insidePlayers.Contains(other.gameObject))
        {
            insidePlayers.Remove(other.gameObject);
            Debug.Log($"Player left there are {insidePlayers.Count} players left inside this door trigger");
        }
    }

    void OpenDoor()
    {
        if (isClosing || isOpening) return;
        
        lerpTime = 0f;
        StartCoroutine(OpenDoorAnim());
    }

    void CloseDoor()
    {
        if (isClosing || isOpening) return;
        
        lerpTime = 0f;
        StartCoroutine(CloseDoorAnim());
    }

    IEnumerator OpenDoorAnim()
    {
        isOpening = true;
        while (lerpTime < 1f)
        {
            lerpTime += Time.deltaTime * openSpeed;

            leftPanel.localPosition = Vector3.Lerp(closedLeft, openLeft, lerpTime);
            rightPanel.localPosition = Vector3.Lerp(closedRight, openRight, lerpTime);
            yield return new WaitForEndOfFrame();
        }

        isOpening = false;
        isOpened = true;
    }

    IEnumerator CloseDoorAnim()
    {
        isClosing = true;
        while (lerpTime < 1f)
        {
            lerpTime += Time.deltaTime * openSpeed;

            leftPanel.localPosition = Vector3.Lerp(openLeft, closedLeft, lerpTime);
            rightPanel.localPosition = Vector3.Lerp(openRight, closedRight, lerpTime);
            yield return new WaitForEndOfFrame();
        }

        isClosing = false;
        isOpened = false;
    }

    void Update()
    {
        MonitorDoor();
    }

    void MonitorDoor()
    {
        if (insidePlayers.Count > 0 && !isOpening && !isOpened)
        {
            OpenDoor();
        }

        if (insidePlayers.Count == 0 && isOpened)
        {
            CloseDoor();
            isOpened = false;
        }
    }
}