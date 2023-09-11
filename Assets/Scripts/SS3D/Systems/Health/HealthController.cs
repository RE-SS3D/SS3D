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

        public event EventHandler OnBodyPartAdded;

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
        /// This will eventually actually attach a bodypart to the body, for now,
        /// only used to warn other stuff that a body part was added.
        /// </summary>
        /// <param name="bodyPart"></param>
        public void AddBodyPart(BodyPart bodyPart)
        {
            OnBodyPartAdded?.Invoke(this, EventArgs.Empty);
        }
    }
}

