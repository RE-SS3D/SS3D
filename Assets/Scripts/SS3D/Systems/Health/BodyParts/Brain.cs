using SS3D.Logging;
using SS3D.Systems.Entities;
using System.Collections;

namespace SS3D.Systems.Health
{
	/// <summary>
	/// When the brain dies, the player dies.
	/// </summary>
	public class Brain : BodyPart
	{
		public float PainAmount { get; private set; }

		protected override void AddInitialLayers()
		{
			TryAddBodyLayer(new CirculatoryLayer(this, 3f));
			TryAddBodyLayer(new NerveLayer(this));
			TryAddBodyLayer(new OrganLayer(this));
			InvokeOnBodyPartLayerAdded();

		}

        public override void OnStartServer()
        {
            base.OnStartServer();
            StartCoroutine(DelayInit());
        }

        /// <summary>
        /// Necessary to prevent issue with body part not getting attached ...
        /// TODO : Implement a proper pipeline of initialisation.
        /// </summary>
        private IEnumerator DelayInit()
        {
            yield return null;

            if (HealthController == null)
            {
                HealthController = GetComponentInParent<HealthController>();
            }
        }

        protected override void AfterSpawningCopiedBodyPart() { }

        /// <summary>
        /// When the brain is destroyed, the player is killed.
        /// </summary>
        protected override void BeforeDestroyingBodyPart()
        {
            Log.Information(this, "brain dies");
            Human entity = GetComponentInParent<Human>();
            entity?.Kill();
        }
	}
}
