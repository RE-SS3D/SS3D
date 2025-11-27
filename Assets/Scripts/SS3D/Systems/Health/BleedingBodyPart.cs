using Coimbra;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Data.Generated;
using System;
using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Should be set up on same game object as all Body part with a circulatory layer if you want to see some bleeding action.
    /// </summary>
    public class BleedingBodyPart : NetworkBehaviour
    {
        [SerializeField]
        private BodyPart _bodyPart;

        [SyncVar(OnChange = nameof(SyncBleedEffect))]
        private bool _isBleeding;

        private GameObject _bloodEffect;

        public bool IsBleeding
        {
            get => _isBleeding;
            set => _isBleeding = value;
        }

        public override void OnStartServer()
        {
            _bodyPart.OnBodyPartDestroyed += HandleBodyPartDestroyedOrDetached;
            _bodyPart.OnBodyPartDetached += HandleBodyPartDestroyedOrDetached;
        }

        public void SyncBleedEffect(bool prev, bool next, bool asServer)
        {
            if (prev == next)
            {
                return;
            }

            if (next && _bloodEffect == null)
            {
                GameObject bleedingEffect = ParticlesEffects.BleedingParticle;
                GameObject bloodDisplayer;
                Transform bloodParent;
                if (_bodyPart.BodyCollider != null)
                {
                    bloodDisplayer = _bodyPart.BodyCollider.gameObject;
                    bloodParent = _bodyPart.BodyCollider.gameObject.transform;
                }
                else
                {
                    bloodDisplayer = gameObject;
                    bloodParent = gameObject.transform;
                }

                _bloodEffect = Instantiate(bleedingEffect, bloodDisplayer.transform.position, Quaternion.identity);
                _bloodEffect.transform.parent = bloodParent;
            }
            else if (!next && _bloodEffect != null)
            {
                _bloodEffect.Dispose(true);
            }
        }

        protected void OnDestroy()
        {
            _bodyPart.OnBodyPartDestroyed -= HandleBodyPartDestroyedOrDetached;
            _bodyPart.OnBodyPartDetached -= HandleBodyPartDestroyedOrDetached;
        }

        private void HandleBodyPartDestroyedOrDetached(object sender, EventArgs eventArgs)
        {
            _isBleeding = false;
        }
    }
}
