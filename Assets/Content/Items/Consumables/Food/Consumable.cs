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


/// <summary>
/// This class handles items that can be consumed
/// they have an substance container which has have it's contents processed when ingested
/// </summary>
public class Consumable : Item, IConsumable
{
    // What's this made of? How much of that will get in the organism per use?
    public SubstanceContainer content;
    public float contentPerUse;
    
    // Does this food leaves trash behind? Like a chocolate bar dropping it's casing
    public GameObject trashPrefab;
    public bool destroyObject;
    
    // This handles the eating sounds
    public AudioSource audio;
    public AudioClip[] sounds;

    // Verb used to display the interaction
    public string consumeVerb = "eat";
    
    // Eat action, the target is there for when you make someone eat that food
    // For now there's no nutriment or digestion
    // TODO: Digestion
    public void ConsumeAction(GameObject origin, GameObject target = null)
    {
        if (!audio.isPlaying && content.CurrentVolume > 0)
        {
            // To avoid more ifs and elses
            if (target == null) target = origin;
            
            // Gets player's audio source
            audio = target.GetComponentInChildren<AudioSource>();
            
            // Randomly select the audio clip and the pitch
            audio.pitch = Random.Range(0.7f,1.5f);
            audio.PlayOneShot(sounds[Random.Range(0, sounds.Length)]);
            
            Item itemInHand = origin.GetComponentInChildren<Hands>().GetItemInHand();
            
            // Does the portion is in the last one? Does it leaves trash? 
            if (content.CurrentVolume == 1 && trashPrefab != null )
            {
                Item trash = Instantiate(trashPrefab, transform.position, transform.rotation).GetComponent<Item>();
                
                NetworkServer.Spawn(trash.gameObject);
                trash.GenerateNewIcon();

                if (itemInHand == null)
                {
                    ItemHelpers.DestroyItem(this);
                }
                else
                {
                    ItemHelpers.ReplaceItem(itemInHand, trash);
                }
            }
            // Last one? Should it destroy the object?
            if (content.CurrentVolume - contentPerUse <= 0 && destroyObject)
            {
                ItemHelpers.DestroyItem(this);
            }
            // Not last one?
            else
            {
                content.RemoveMoles(contentPerUse);
                Debug.Log(target.name + " consumed " + transform.name);
            }
        }
    }
    
    public bool CanConsume()
    {
        if (audio == null)
        {
            audio = GetComponent<AudioSource>();
        }
        if (!audio.isPlaying && content.CurrentVolume > 0)
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
            list.Add(new ConsumeInteraction {icon = sprite, verb = consumeVerb});
        }

        return list.ToArray();
    }

    public override void CreateInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
    {
        base.CreateInteractions(targets, interactions);
        
        // Adds the interaction if it's not playing
        if (CanConsume())
        {
            interactions.Add(new InteractionEntry(targets[0],new ConsumeInteraction {icon = sprite, verb = consumeVerb}));
        }
    }

    private class ConsumeInteraction : IInteraction
    {
        public Sprite icon;
        public string verb;

        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return verb;
        }

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return icon;
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            // easy access to shit
            GameObject source = interactionEvent.Source?.GetComponentInTree<Creature>().gameObject;
            GameObject target = interactionEvent.Target.GetComponent<Transform>().gameObject; 
            
            // you can only interact with consumables or creatures
            if (target.GetComponent(typeof(Consumable)) || source.GetComponent(typeof(Creature)))
            {
                if (!InteractionExtensions.RangeCheck(interactionEvent))
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            GameObject source = interactionEvent.Source?.GetComponentInTree<Creature>().gameObject;
            GameObject target = interactionEvent.Target.GetComponent<Transform>().gameObject;            
            Consumable itemInHand = source?.GetComponentInChildren<Hands>().GetItemInHand()?.GetComponent<Consumable>();
	        
			//Debug.Log("source:  " + source.name + " target: " + target?.name + " item: " + itemInHand?.gameObject.name);
			
            // Item in hand and interacting with origin
            if (target == null) 
            {
                itemInHand.ConsumeAction(source);
            }
            // Item not in hand
            if (target.GetComponent(typeof(Consumable)))
            {
                target.GetComponent<IConsumable>().ConsumeAction(source);
            }
            // Item in hand and interacting with other player
            if (target.GetComponent(typeof(Creature)))
            {
                itemInHand.GetComponent<IConsumable>().ConsumeAction(source, target);
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
