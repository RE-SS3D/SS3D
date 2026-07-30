using UnityEngine;
using FishNet.Object;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using Coimbra;
using SS3D.Systems.Health;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using System.Collections.Generic;
using System;
using System.Diagnostics.Tracing;
using System.Linq;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Has a reference towards everything related to health on a human player.
    /// </summary>
    public class HealthController : NetworkBehaviour
    {

        [SerializeField]
        private CirculatoryController _circulatoryController;

        [SerializeField]
        private FeetController _feetController;

        public CirculatoryController Circulatory => _circulatoryController;

        public FeetController FeetController => _feetController;

        /// <summary>
        /// Every body part making up this entity: the external parts found at spawn, plus internal organs as they
        /// attach. Kept accurate in both directions - parts are removed again when destroyed or detached, including
        /// the organs inside a part that is removed.
        /// Membership doubles as the record of which parts this controller is hooked to: a part is subscribed exactly
        /// while it is in here, added and unsubscribed in the same breath, so the two cannot disagree and no second
        /// collection is needed to track it.
        /// Careful when changing what goes in here: Lungs.SetBreathingState sums oxygen demand across this set to
        /// decide breathing state, so membership directly moves the breathing thresholds.
        /// A set rather than a list so "no duplicates" is enforced by the type instead of by every caller remembering
        /// to check first - a part tracked twice would silently double its share of the body's oxygen demand.
        /// </summary>
        private readonly HashSet<BodyPart> _bodyPartsOnEntity = new HashSet<BodyPart>();

        public IReadOnlyCollection<BodyPart> BodyPartsOnEntity => _bodyPartsOnEntity;

        public event EventHandler<BodyPart> OnBodyPartRemoved;

        public event EventHandler<BodyPart> OnBodyPartAdded;

        public float BodyPartsVolume 
        {
            get
            {
                BodyPart[] allBodyParts = GetComponentsInChildren<BodyPart>();
                return (float)allBodyParts.Sum(x => x.Volume);
            }
            
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            // Route the initial scan through AddBodyPart so the two entry points cannot drift apart. Internal organs
            // are not found here - they attach later and announce themselves through the same method.
            foreach (BodyPart part in GetComponentsInChildren<BodyPart>())
            {
                AddBodyPart(part);
            }
        }

        private void HandleBodyPartDestroyedOrDetached(object sender, EventArgs eventArgs)
        {
            BodyPart bodyPart = (BodyPart)sender;

            // Drop the part before announcing it, so a listener reading BodyPartsOnEntity during the event sees the
            // body as it now is. Remove reports whether it was actually tracked, which makes this idempotent - in
            // practice a part fires only one of destroyed/detached, but nothing guarantees that - and unsubscribing
            // lets the part be tracked again if it is ever reattached.
            if (_bodyPartsOnEntity.Remove(bodyPart))
            {
                bodyPart.OnBodyPartDestroyed -= HandleBodyPartDestroyedOrDetached;
                bodyPart.OnBodyPartDetached -= HandleBodyPartDestroyedOrDetached;
            }

            OnBodyPartRemoved?.Invoke(this, bodyPart);
        }

        /// <summary>
        /// Track a body part as belonging to this entity: the external parts at spawn, and each internal organ as it
        /// attaches and announces itself. Subscribes to the part's removal so it is dropped again on destroy or
        /// detach, then notifies listeners - the circulatory controller re-derives its perfused set from this.
        /// Safe to call more than once for the same part.
        /// </summary>
        /// <param name="bodyPart">The body part that was attached.</param>
        public void AddBodyPart(BodyPart bodyPart)
        {
            if (!bodyPart)
            {
                return;
            }

            // Add reports whether the part was new, so tracking and subscribing stay in lockstep off one lookup.
            if (_bodyPartsOnEntity.Add(bodyPart))
            {
                bodyPart.OnBodyPartDestroyed += HandleBodyPartDestroyedOrDetached;
                bodyPart.OnBodyPartDetached += HandleBodyPartDestroyedOrDetached;
            }

            OnBodyPartAdded?.Invoke(this, bodyPart);
        }
    }
}

