using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using Mirror;
using Random = System.Random;

public class AirlockManager : MonoBehaviour
{
    [Header("Type")] [SerializeField] Types type;

    protected enum LightsState
    {
        standby,
        opening,
        closing,
        denied,
        broken,
    };

    enum Types
    {
        regular,
        dual
    };

    MaterialChanger changer = null;

    //MaterialChanger changer = 
    // 0 for green
    // 1 for red
    // 2 for off

    [Header("Sounds")] // TODO move these to some global settings somewhere
    [SerializeField]
    private AudioClip openSound = null;

    [SerializeField] private AudioClip closeSound = null;

    [Header("Doors")] [SerializeField] private Transform left = null;
    [SerializeField] private Transform right = null;

    [Header("Access")] [SerializeField] string idTag;

    [Header("Power")] [SerializeField] bool powered;

    [Header("Lights")] [SerializeField] bool lightOn;
    [SerializeField] MeshRenderer[] lights;
    [SerializeField] Material[] lightsMaterials;
    [SerializeField] int lightsBrokenFlicksCount = 10;
    [SerializeField] float lightsBrokenFlicksInterval = .2f;

    [Header("States")] [SerializeField] private bool open = false;
    [SerializeField] private bool moving = false;
    [SerializeField] private bool welded = false;
    [SerializeField] private bool bolted = false;
    [SerializeField] private bool disabled = false;
    [SerializeField] private bool autoClose = true;

    [Header("Animation Settings")] [SerializeField]
    private Vector3 openAxis = Vector3.right;

    [SerializeField] private float movingDistance = 1.0f;
    [SerializeField] private float movingTime = 1.0f;

    [SerializeField] private float targetOpen = 0.45f;
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SerializeField] private AudioSource audioSource;

    [SerializeField] private float closeTimer = 3f;
    [SerializeField] private float closeTimerLeft = 0f;

    private Coroutine movingCoroutine;

    public bool Stuck => welded || bolted || disabled || !powered; // TODO: add electrical.off here

    void Start()
    {
        changer = gameObject.AddComponent(typeof(MaterialChanger)) as MaterialChanger;

        LightsSetState();
    }

    void Update()
    {
        if (autoClose && open && !moving && powered)
        {
            closeTimerLeft -= Time.deltaTime;

            if (closeTimerLeft <= 0 && !Close())
            {
                closeTimerLeft += closeTimer / 3;
            }
        }
    }

    public bool HasAccess(string inIdTag)
    {
        return inIdTag == idTag;
    }

    public bool TryOpen(GameObject initiator)
    {
        if (HasAccess(idTag))
        {
            return Open();
        }

        LightsSetState(LightsState.denied);
        return false;
    }

    public bool TryClose(GameObject initiator)
    {
        if (HasAccess(idTag))
        {
            return Close();
        }

        LightsSetState(LightsState.denied);
        return false;
    }

    public bool TryToggle(GameObject initiator)
    {
        return open ? TryClose(initiator) : TryOpen(initiator);
    }

    protected bool Open(bool force = false)
    {
        if (force || CanOpen())
        {
            if (movingCoroutine != null)
            {
                StopCoroutine(movingCoroutine);
                movingCoroutine = null;
            }
            
            closeTimerLeft = closeTimer;
            StartMoving();
            return true;
        }

        return false;
    }

    protected bool Close(bool force = false)
    {
        if (force || CanClose())
        {
            StartMoving();
            return true;
        }

        return false;
    }

    protected bool Toggle(bool force = false)
    {
        return open ? Close(force) : Open(force);
    }

    protected bool CanOpen()
    {
        return !Stuck && !moving && !open;
    }

    protected bool CanClose()
    {
        return !Stuck && !moving && open && IsClosingWayEmpty();
    }

    //TODO: Realize checking objects on the way.
    protected bool IsClosingWayEmpty()
    {
        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryOpen(other.gameObject);
    }

    private void StartMoving()
    {
        movingCoroutine = StartCoroutine(ProcessMoving());
    }

    [SerializeField]
    private IEnumerator ProcessMoving() // TODO make a more performant, declarative manager or these simple animations
    {
        moving = true;

        var origin = open ? targetOpen : 0f;
        var target = open ? 0f : targetOpen;

        LightsSetState(open ? LightsState.closing : LightsState.opening);

        audioSource.clip = open ? openSound : closeSound;
        audioSource.Play();

        open = !open;

        for (var time = 0f; time <= movingTime; time += Time.deltaTime)
        {
            var t = Mathf.Lerp(origin, target, time / movingTime);

            left.transform.localPosition = openAxis * movingDistance * openCurve.Evaluate(t);
            right.transform.localPosition = -openAxis * movingDistance * openCurve.Evaluate(t);

            yield return null;
        }

        left.transform.localPosition = openAxis * movingDistance * openCurve.Evaluate(target);
        right.transform.localPosition = -openAxis * movingDistance * openCurve.Evaluate(target);
        
        LightsSetState();

        moving = false;
    }

    private void OnMouseUp()
    {
        Toggle(); // TODO: Change to TryToggle()
    }

    protected void LightsSetState(LightsState state = LightsState.standby)
    {
        StartCoroutine(LightsSetStateCoroutine(state));
    }

    private IEnumerator LightsSetStateCoroutine(LightsState state)
    {
        switch (state)
        {
            case LightsState.standby:
                changer.ChangeMaterial(lights[0], lightsMaterials[2]);
                changer.ChangeMaterial(lights[1], lightsMaterials[2]);
                break;
            case LightsState.opening:
            case LightsState.closing:
                changer.ChangeMaterial(lights[0], lightsMaterials[0]);
                changer.ChangeMaterial(lights[1], lightsMaterials[0]);
                break;

            case LightsState.broken:
                var step = 0;
                while (step < lightsBrokenFlicksCount)
                {
                    changer.ChangeMaterial(lights[0], lightsMaterials[UnityEngine.Random.Range(0, 2)]);
                    changer.ChangeMaterial(lights[1], lightsMaterials[UnityEngine.Random.Range(0, 2)]);
                    step++;
                    yield return new WaitForSeconds(lightsBrokenFlicksInterval);
                }

                break;

            case LightsState.denied:
                changer.ChangeMaterial(lights[0], lightsMaterials[1]);
                changer.ChangeMaterial(lights[1], lightsMaterials[1]);
                yield return new WaitForSeconds(1);
                changer.ChangeMaterial(lights[0], lightsMaterials[2]);
                changer.ChangeMaterial(lights[1], lightsMaterials[2]);
                break;
        }
    }
}