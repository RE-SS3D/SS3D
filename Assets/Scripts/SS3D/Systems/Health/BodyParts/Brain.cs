using SS3D.Logging;
using SS3D.Systems.Entities;

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

		protected override void DestroyBodyPart()
		{
            Punpun.Information(this, "brain dies");
			Human entity = GetComponentInParent<Human>();
			entity?.Kill();
			InvokeOnBodyPartDestroyed();
			Dispose(true);
		}
	}
}
