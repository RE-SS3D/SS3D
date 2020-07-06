using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using SS3D.Engine.Inventory.Extensions;
using SS3D.Engine.Substances;
using UnityEngine;

public class Can : Item, IConsumable
{
    public int contentPerSip = 2; 
    public SubstanceContainer content;
    
    // This handles the eating sounds
    public AudioSource audio;
    public AudioClip[] sounds;

    // Eat action, the target is there for when you make someone eat that food
    // For now there's no nutriment or digestion
    
    // TODO: Digestion
    public void ConsumeAction(GameObject target)
    {
        if (!audio.isPlaying && content.CurrentVolume != 0)
        {
            // Gets player's audio source
            audio = target.GetComponentInChildren<AudioSource>();

            // Randomly select the audio clip and the pitch
            audio.pitch = Random.Range(0.7f, 1.5f);
            audio.PlayOneShot(sounds[Random.Range(0, sounds.Length)]);

            content.RemoveMoles(contentPerSip);
            Debug.Log(target.name + " Took a sip");
        }
    }

    public bool CanConsume()
    {
        if (audio == null)
        {
            audio = GetComponent<AudioSource>();
        }
        if (!audio.isPlaying && content.Volume > 0)
        {
            return true;
        }

        return false;
    }
    
    public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
    {
        List<IInteraction> list = base.GenerateInteractions(interactionEvent).ToList();
     
        // Adds the interaction if it's not playing
        if (CanConsume())
        {
            list.Add(new EatInteraction {icon = sprite});
        }

        return list.ToArray();
    }

    public override void CreateInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
    {
        base.CreateInteractions(targets, interactions);
        
        // Adds the interaction if it's not playing
        if (CanConsume())
        {
            interactions.Add(new InteractionEntry(targets[0],new EatInteraction {icon = sprite}));
        }
    }

    private class EatInteraction : IInteraction
    {
        public Sprite icon;

        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return "Drink";
        }

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return icon;
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is Can can)
            {
                if (!InteractionExtensions.RangeCheck(interactionEvent))
                {
                    return false;
                }
            }

            return true;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Target is Can can)
            {
                // Gets the player lol crappy code I kno
                can.ConsumeAction(interactionEvent.Source.GetComponentInTree<Transform>().gameObject);
            }    
            return false;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            throw new System.NotImplementedException();
        }

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            throw new System.NotImplementedException();
        }
    }
}
