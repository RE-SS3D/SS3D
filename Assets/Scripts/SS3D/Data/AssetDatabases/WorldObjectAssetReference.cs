using JetBrains.Annotations;
using SS3D.Attributes;
using UnityEditor.VersionControl;
using UnityEngine;

namespace SS3D.Data.AssetDatabases
{
	/// <summary>
	/// This SO is used to reference world object assets in inspector fields without worrying about losing data.
	/// World object assets are anything that can be placed in the world as a GameObject, like items and tileobjects.
	/// </summary>
	public sealed class WorldObjectAssetReference : ScriptableObject
	{
		/// <summary>
		/// The prefab of this asset reference. Here for quick access.
		/// </summary>
		[CanBeNull]
		public GameObject Prefab => Assets.Get<GameObject>(Database, Id);

        /// <summary>
        /// Short access to the database asset.
        /// </summary>
        public DatabaseAsset Asset => new DatabaseAsset(Id, Database);

        public T Get<T>() where T : Object => Asset.Get<T>();

#if UNITY_EDITOR
		[ReadOnly]
#endif
		[Header("This file is auto-generated, do not modify it manually")]
		public string Id;

#if UNITY_EDITOR
		[ReadOnly]
#endif
		[Header("This file is auto-generated, do not modify it manually")]
		public string Database;
	}
}