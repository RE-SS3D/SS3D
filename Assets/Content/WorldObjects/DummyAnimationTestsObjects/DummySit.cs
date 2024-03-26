using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummySit : MonoBehaviour
{

    public DummyAnimatorController animatorController;

    public CharacterController characterController;

    public DummyMovement movement;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.J))
            return;

        if (GetComponent<DummyPositionController>().Position == PositionType.Standing)
        {
            TrySit();
        }
        else if(GetComponent<DummyPositionController>().Position == PositionType.Sitting)
        {
            StopSitting();
        }
       
    }

    private void TrySit()
    {
        // Cast a ray from the mouse position into the scene
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        // Check if the ray hits any collider
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Check if the collider belongs to a GameObject
            GameObject obj = hit.collider.gameObject;

            if (obj.TryGetComponent(out DummySittable sit) && GoodDistanceFromRootToSit(sit.transform))
            {
                StartCoroutine(Sit(sit.orientation));
            }
            
        }
    }

    private IEnumerator Sit(Transform sitOrientation)
    {
        movement.enabled = false;
        characterController.enabled = false;
        
        animatorController.Sit(true);

        Vector3 initialRotation = transform.eulerAngles;
        
        Vector3 initialPosition = transform.position;

        StartCoroutine(CoroutineHelper.ModifyVector3OverTime(x => transform.eulerAngles = x,
            initialRotation, sitOrientation.eulerAngles,0.5f));
        
        yield return (CoroutineHelper.ModifyVector3OverTime(x => transform.position = x,
            initialPosition, sitOrientation.position,0.5f));
        
        GetComponent<DummyPositionController>().Position = PositionType.Sitting;
    }

    private void StopSitting()
    {
        movement.enabled = true;
        characterController.enabled = true;
        animatorController.Sit(false);
        GetComponent<DummyPositionController>().Position = PositionType.Standing;
    }

    private bool GoodDistanceFromRootToSit(Transform sit)
    {
        return Vector3.Distance(transform.position, sit.position) < 0.8f;
    }
}
