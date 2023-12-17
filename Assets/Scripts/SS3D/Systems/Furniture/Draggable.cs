using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Furniture;
using SS3D.Systems.Inventory.Interactions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Draggable : MonoBehaviour, IInteractionTarget
{
    /// <summary>
    /// The thing that drag this object
    /// </summary>
    [SerializeField]
    private Transform _dragger;

    /// <summary>
    /// The distance at which the dragged item should stay from dragger
    /// </summary>
    [SerializeField]
    private float _distanceFromDragger = 1f;

    /// <summary>
    /// True if the object is currently being dragged.
    /// </summary>
    private bool _dragged;

    public bool Dragged => _dragged;

    public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
    {
        DragInteraction dragInteraction = new DragInteraction();

        return new IInteraction[] { dragInteraction };
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!_dragged) return;
        
        gameObject.transform.position = _dragger.transform.forward * _distanceFromDragger;
    }
}
