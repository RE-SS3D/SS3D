using System.Collections.Generic;
using System.Linq;
using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using UnityEngine;

namespace SS3D.Content.Items.Cosmetic
{
    public class Boombox : Item, IToggleable
    {
        [SerializeField]
        private AudioClip[] musics;
        private AudioSource audioSource;
        public bool radioOn;
        public int currentMusic;

        private bool toEnable = false;
        private float enableTime;

        private void OnEnable()
        {
            if (toEnable)
            {
                toEnable = false;
                audioSource.time = enableTime;
                audioSource.Play();
            }
        }

        private void OnDisable()
        {
            if (radioOn)
            {
                toEnable = true;
                enableTime = audioSource.time;
            }
        }

        private class ChangeMusic : IInteraction
        {
            public IClientInteraction CreateClient(InteractionEvent interactionEvent)
            {
                return null;
            }

            public string GetName(InteractionEvent interactionEvent)
            {
                return "Change Music";
            }

            public bool CanInteract(InteractionEvent interactionEvent)
            {
                if (interactionEvent.Target is Boombox boom)
                {
                    if (!InteractionExtensions.RangeCheck(interactionEvent))
                    {
                        return false;
                    }
                    return boom.radioOn;
                }
                if (interactionEvent.Source is Boombox boom1)
                {
                    return boom1.radioOn;
                }

                return false;
            }

            public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
            {
                if (interactionEvent.Target is Boombox boom)
                {
                    boom.ChangeCurrentMusic();
                }
                if (interactionEvent.Source is Boombox boom1)
                {
                    boom1.ChangeCurrentMusic();
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

        void Start()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = musics[0];
        }

        public void Toggle()
        {
            radioOn = !radioOn;
            if (!radioOn)
            {
                audioSource.Stop();
            }
            else
            {
                audioSource.Play();
            }
            RpcTurnOn(radioOn);
        }

        public void ChangeCurrentMusic()
        {
            audioSource.Stop();
            enableTime = 0f;
            if (currentMusic < musics.Length-1)
            {
                currentMusic++;
            } else
            {
                currentMusic = 0;
            }
            audioSource.clip = musics[currentMusic];
            audioSource.Play();
            RpcChangeMusic(currentMusic);
        }

        [ClientRpc]
        private void RpcChangeMusic(int value)
        {
            audioSource.Stop();
            audioSource.clip = musics[value];
            audioSource.Play();
        }

        [ClientRpc]
        private void RpcTurnOn(bool value)
        {
            if (!value)
            {
                audioSource.Stop();
            }
            else
            {
                audioSource.Play();
            }
        }

        public bool GetState()
        {
            return radioOn;
        }

        public override IInteraction[] GenerateInteractions(IInteractionTarget[] targets)
        {
            List<IInteraction> interactions = base.GenerateInteractions(targets).ToList();
            interactions.Insert(0, new ChangeMusic());
            ToggleInteraction toggleInteraction = new ToggleInteraction { OnName = "Turn off", OffName = "Turn on" };
            interactions.Insert(GetState() ? 1 : 0, toggleInteraction);
            return interactions.ToArray();
        }
    }
}