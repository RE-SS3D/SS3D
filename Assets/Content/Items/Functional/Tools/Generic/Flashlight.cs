using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Inventory;

namespace SS3D.Content.Items.Functional.Tools
{
    public class Flashlight : Item
    {
        [SerializeField]
        public new Light light = null;
        public Sprite turnOnIcon;

        // Materials to display on bulb object when on / off
        public Material onMaterial;
        public Material offMaterial;

        // Reference to bulb object within flashlight
        public GameObject bulbObject;


        [System.NonSerialized]
        public MeshRenderer bulbRenderer;
        
        public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            // Retrieves renderer from bulb object
            this.bulbRenderer = bulbObject.GetComponent<MeshRenderer>();
                
            List<IInteraction> list = base.GenerateInteractions(interactionEvent).ToList();
            list.Add(new FlashlightInteraction{
                icon = turnOnIcon,
                bulbRenderer = bulbRenderer,
                onMaterial = onMaterial,
                offMaterial = offMaterial
            });
            return list.ToArray();
        }
    }
}