using Coimbra.Services.Events;
using UnityEngine;

namespace SS3D.Core
{
	/// <summary>
	/// Interface to provide documentation for Actors.
	/// </summary>
	public interface IActor
	{
		/// <summary>
		/// The instance ID for an Actor's GameObject.
		/// </summary>
		public int Id { get; }

		/// <summary>
		/// The cached GameObject associated with this Actor.
		/// </summary>
		public GameObject GameObject { get; }

		/// <summary>
		/// The cached Transform associated with this Actor.
		/// </summary>
		public Transform Transform { get; }

		/// <summary>
		/// Casts the Transform property into a RectTransform.
		/// </summary>
		public RectTransform RectTransform { get; }

		/// <summary>
		/// Returns if the object is Active-self (Active locally even if not active in hierarchy).
		/// </summary>
		public bool ActiveSelf { get; }

		/// <summary>
		/// Returns if the object is active in the hierarchy.
		/// </summary>
		public bool ActiveInHierarchy { get; }

		/// <summary>
		/// Returns this Actor's GameObject's parent.
		/// </summary>
		public Transform Parent { get; }

		/// <summary>
		/// Returns the transform.forward of the Actor's GameObject.
		/// </summary>
		public Vector3 Forward { get; }

		/// <summary>
		/// Returns the negative transform.forward of the Actor's GameObject. 
		/// </summary>
		public Vector3 Backward { get; }

		/// <summary>
		/// Returns the transform.right of the Actor's GameObject.
		/// </summary>
		public Vector3 Right { get; }

		/// <summary>
		/// Returns the negative transform.right of the Actor's GameObject.
		/// </summary>
		public Vector3 Left { get; }

		/// <summary>
		/// Returns the transform.up of the Actor's GameObject.
		/// </summary>
		public Vector3 Up { get; }

		/// <summary>
		/// Returns the negative transform.up of the Actor's GameObject.
		/// </summary>
		public Vector3 Down { get; }

		/// <summary>
		/// Returns the Root transform associated with the Actor's GameObject.
		/// </summary>
		public Transform Root { get; }

		/// <summary>
		/// Returns the scale of this Actor's GameObject.
		/// </summary>
		public Vector3 Scale { get; }

		/// <summary>
		/// Returns the LocalToWorld matrix from the Actor's transform.
		/// </summary>
		public Matrix4x4 LocalToWorldMatrix { get; }

		/// <summary>
		/// Returns the WorldToLocal matrix from the Actor's transform.
		/// </summary>
		public Matrix4x4 WorldToLocalMatrix { get; }

		/// <summary>
		/// Getter and Setter for the Position of the Actor's transform. 
		/// </summary>
		public Vector3 Position { get; set; }

		/// <summary>
		/// Getter and Setter for the LocalPosition of the Actor's transform.
		/// </summary>
		public Vector3 LocalPosition { get; set; }

		/// <summary>
		/// Getter and Setter in Euler angles for the Actor's transform rotation.
		/// </summary>
		public Vector3 RotationEuler { get; set; }

		/// <summary>
		/// Getter and Setter in Quaternion angles for the Actor's transform rotation. 
		/// </summary>
		public Quaternion Rotation { get; set; }

		/// <summary>
		/// Getter and Setter in Euler angles for the Actor's transform local rotation.
		/// </summary>
		public Vector3 LocalEuler { get; set; }

		/// <summary>
		/// Getter and Setter in Quaternion angles for the Actor's transform local rotation.
		/// </summary>
		public Quaternion LocalRotation { get; set; }

		/// <summary>
		/// Getter and Setter for the local scale on the Actor's transform.
		/// </summary>
		public Vector3 LocalScale { get; set; }

		/// <summary>
		/// Initializes the Actor, caching the Transform and the GameObject.
		/// </summary>
		private void Initialize() { }

		/// <summary>
		/// Sets this Actor's GameObject active state.
		/// </summary>
		/// <param name="state">The intended state to set to.</param>
		public void SetActive(bool state) { }

		/// <summary>
		/// Sets this Actor's parent.
		/// </summary>
		/// <param name="parent">The Actor's transform new parent.</param>
		public void SetParent(Transform parent) { }

		/// <summary>
		/// Rotates the transform so the forward vector points at target's current position using a Transform target.
		/// </summary>
		/// <param name="target">Target to look at.</param>
		public void LookAt(Transform target) { }

		/// <summary>                         
		/// Rotates the transform so the forward vector points at target's current position using a Vector3 target.
		/// </summary>
		/// <param name="target">Target to look at.</param>
		public void LookAt(Vector3 target) { }

		/// <summary>
		/// Adds and IEvent from the CS Framework listener to a list of event handles. This ensures the handles are removed when the object is destroyed.
		/// </summary>
		/// <param name="handle">The event handle.</param>
		public void AddHandle(EventHandle handle) { }
	}
}