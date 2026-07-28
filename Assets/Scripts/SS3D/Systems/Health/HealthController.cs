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
using System.Collections.ObjectModel;
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

        private List<BodyPart> _bodyPartsOnEntity = new List<BodyPart>();

        public ReadOnlyCollection<BodyPart> BodyPartsOnEntity => _bodyPartsOnEntity.AsReadOnly();

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
            _bodyPartsOnEntity.AddRange(GetComponentsInChildren<BodyPart>());
            foreach (BodyPart part in _bodyPartsOnEntity)
            {
                part.OnBodyPartDestroyed += HandleBodyPartDestroyedOrDetached;
                part.OnBodyPartDetached += HandleBodyPartDestroyedOrDetached;
            }
        }

        private void HandleBodyPartDestroyedOrDetached(object sender, EventArgs eventArgs)
        {
            OnBodyPartRemoved?.Invoke(this, (BodyPart)sender);
        }

        /// <summary>
        /// Register a body part that attached after the initial spawn scan - notably an async-spawned internal organ
        /// (heart, lungs, brain), which the transform-based scan in OnStartServer cannot see. Track it and subscribe to
        /// its removal so it is dropped again on destroy/detach, then notify listeners (e.g. the circulatory controller
        /// re-derives its perfused set). Safe to call more than once for the same part.
        /// </summary>
        /// <param name="bodyPart">The body part that was attached.</param>
        public void AddBodyPart(BodyPart bodyPart)
        {
            if (bodyPart != null && !_bodyPartsOnEntity.Contains(bodyPart))
            {
                _bodyPartsOnEntity.Add(bodyPart);
                bodyPart.OnBodyPartDestroyed += HandleBodyPartDestroyedOrDetached;
                bodyPart.OnBodyPartDetached += HandleBodyPartDestroyedOrDetached;
            }

            OnBodyPartAdded?.Invoke(this, bodyPart);
        }
    }
}

