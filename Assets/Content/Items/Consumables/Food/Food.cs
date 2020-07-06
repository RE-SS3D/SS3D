using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using SS3D.Engine.Inventory.Extensions;
using UnityEngine;

public class Food : Item, IConsumable
{
    // How many bites it'll take you to chomp it all?
    public int portions = 3;
    public int nutrimentPerPortion = 2;
    
    // Does this food leaves trash behind? Like a chocolate bar dropping it's casing
    public GameObject trashPrefab;
    public bool destroyObject;
    
    // This handles the eating sounds
    public AudioSource audio;
    public AudioClip[] sounds;

    // Eat action, the target is there for when you make someone eat that food
    // For now there's no nutriment or digestion
    
    // TODO: Digestion
    public void ConsumeAction(GameObject target)
    {
        if (!audio.isPlaying && portions != 0)
        {
            // Gets player's audio source
            audio = target.GetComponentInChildren<AudioSource>();
            
            // Randomly select the audio clip and the pitch
            audio.pitch = Random.Range(0.7f,1.5f);
            audio.PlayOneShot(sounds[Random.Range(0, sounds.Length - 1)]);
            
            Item itemInHand = target.GetComponentInChildren<Hands>().GetItemInHand();
            
            //Does it leaves trash? Does the portion is in the last one?
            if (trashPrefab != null && portions == 1)
            {
                GameObject trash = Instantiate(trashPrefab, transform.position, transform.rotation);
                NetworkServer.Spawn(trash);
                ItemHelpers.ReplaceItem(itemInHand, trash.GetComponent<Item>());
            }
            // Last one?
            if (portions == 1)
            {
                if (destroyObject)
                    ItemHelpers.DestroyItem(gameObject.GetComponent<Item>());
            }
            // Not last one?
            else
            {
                portions--;
                Debug.Log(target.name + " Took a bite");
            }
        }
    }
    
    public bool CanConsume()
    {
        if (audio == null)
        {
            audio = GetComponent<AudioSource>();
        }
        if (!audio.isPlaying && portions > 0)
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
            return "Eat";
        }

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return icon;
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is Food food)
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
            if (interactionEvent.Target is Food food)
            {
                // Gets the player lol crappy code I kno
                food.ConsumeAction(interactionEvent.Source.GetComponentInTree<Transform>().gameObject);
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
