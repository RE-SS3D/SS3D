using FishNet.Object;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Has a reference towards everything related to health on a human player.
    /// </summary>
    public class HealthController : NetworkBehaviour
    {
        public event EventHandler<BodyPart> OnBodyPartRemoved;

        public event EventHandler OnBodyPartAdded;

        private readonly List<BodyPart> _bodyPartsOnEntity = new();

        [SerializeField]
        private CirculatoryController _circulatoryController;

        [SerializeField]
        private FeetController _feetController;

        public CirculatoryController Circulatory => _circulatoryController;

        public FeetController FeetController => _feetController;

        public ReadOnlyCollection<BodyPart> BodyPartsOnEntity => _bodyPartsOnEntity.AsReadOnly();

        public float BodyPartsVolume
        {
            get
            {
                BodyPart[] allBodyparts = GetComponentsInChildren<BodyPart>();
                return (float)allBodyparts.Sum(x => x.Volume);
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

        /// <summary>
        /// This will eventually actually attach a bodypart to the body, for now,
        /// only used to warn other stuff that a body part was added.
        /// </summary>
        /// <param name="bodyPart"></param>
        public void AddBodyPart(BodyPart bodyPart)
        {
            OnBodyPartAdded?.Invoke(this, EventArgs.Empty);
        }

        private void HandleBodyPartDestroyedOrDetached(object sender, EventArgs eventArgs)
        {
            OnBodyPartRemoved?.Invoke(this, (BodyPart)sender);
        }
    }
}
