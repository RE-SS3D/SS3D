using InspectorGadgets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyInteract : MonoBehaviour
{
    
    public DummyHands hands;
    public Transform lookAtTargetLocker;
    public Transform hips;

    public float interactionMoveDuration;
    
    public bool UnderMaxDistanceFromHips(Vector3 position) => Vector3.Distance(hips.position, position) < 1.3f;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!Input.GetMouseButtonDown(1))
            return;
        
        if (hands.SelectedHand.Full)
        {
            TryInteract();
        }
    }
    
    private void TryInteract()
    {
        // Cast a ray from the mouse position into the scene
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Check if the ray hits any collider
        if (Physics.Raycast(ray, out RaycastHit hit) && UnderMaxDistanceFromHips(hit.point))
        {
            // Check if the collider belongs to a GameObject
            GameObject obj = hit.collider.gameObject;
            StartCoroutine(Interact(obj.transform));
        }
        
        
    }

    private IEnumerator Interact(Transform interactionTarget)
    {
        DummyItem tool = hands.SelectedHand.item;
        Vector3 startPosition = tool.transform.position;
        Vector3 endPosition = interactionTarget.position;
        
        yield return CoroutineHelper.ModifyVector3OverTime(x => 
            tool.transform.position = x,  startPosition, endPosition, interactionMoveDuration);
    }
}
