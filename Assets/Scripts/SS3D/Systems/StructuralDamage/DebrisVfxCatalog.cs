using UnityEngine;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// The packed debris sprite atlas, loaded from Resources. Built by <c>DebrisVfxSetup</c> under
    /// <b>SS3D → Structural Damage → Run Content Prefab Recipes</b>.
    /// </summary>
    public sealed class DebrisVfxCatalog : ScriptableObject
    {
        [SerializeField] private Texture2D _atlas;

        private static DebrisVfxCatalog _instance;

        public static DebrisVfxCatalog Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<DebrisVfxCatalog>(nameof(DebrisVfxCatalog));
                return _instance;
            }
        }

        public Texture2D Atlas => _atlas;
    }
}
