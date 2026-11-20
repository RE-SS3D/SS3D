using Coimbra;
using System;
using UnityEngine;

namespace SS3D.Systems.IdAccess
{
    /// <summary>
    /// The colour of each <see cref="Department"/>, for anything in the world that shows a department in colour: floor
    /// paint so far. Edit them in Project Settings › SS3D › Assets › Department Colors; painted floors redraw at once.
    /// <para>
    /// Colours are sRGB, as the colour picker shows them. The lobby's UI Toolkit tokens (<c>LobbyTokens.uss</c>
    /// <c>--dept-*</c>) started from the same values but are kept by hand, since USS can't read this asset.
    /// </para>
    /// </summary>
    [ProjectSettings("SS3D/Assets", "Department Colors")]
    public sealed class DepartmentColors : ScriptableSettings
    {
        [SerializeField]
        private Color _command = DefaultOf(Department.Command);

        [SerializeField]
        private Color _security = DefaultOf(Department.Security);

        [SerializeField]
        private Color _engineering = DefaultOf(Department.Engineering);

        [SerializeField]
        private Color _medical = DefaultOf(Department.Medical);

        [SerializeField]
        private Color _science = DefaultOf(Department.Science);

        [SerializeField]
        private Color _cargo = DefaultOf(Department.Cargo);

        [SerializeField]
        private Color _service = DefaultOf(Department.Service);

        [SerializeField]
        private Color _civilian = DefaultOf(Department.Civilian);

        /// <summary>Raised in the Editor after a colour is edited, so anything drawn from these colours can redraw.</summary>
        public static event Action Changed;

        /// <summary>A department's colour from the settings asset, or its default until the asset is loaded. White for none.</summary>
        public static Color Get(Department department)
        {
            DepartmentColors settings = GetOrFind<DepartmentColors>();
            return settings != null ? settings.ColorOf(department) : DefaultOf(department);
        }

        /// <summary>The colours the project started with: the lobby's department tokens.</summary>
        public static Color DefaultOf(Department department) => department switch
        {
            Department.Command => Hex(0x8fb4f2),
            Department.Security => Hex(0xf291a8),
            Department.Engineering => Hex(0xecd35e),
            Department.Medical => Hex(0x62d0d6),
            Department.Science => Hex(0xcf9df0),
            Department.Cargo => Hex(0xc9a27c),
            Department.Service => Hex(0x8edb9c),
            Department.Civilian => Hex(0xddd7cb),
            _ => Color.white,
        };

        protected override void OnValidate()
        {
            base.OnValidate();
#if UNITY_EDITOR
            // After the inspector finishes: listeners rebuild textures, which OnValidate shouldn't do itself.
            UnityEditor.EditorApplication.delayCall += () => Changed?.Invoke();
#endif
        }

        private Color ColorOf(Department department) => department switch
        {
            Department.Command => _command,
            Department.Security => _security,
            Department.Engineering => _engineering,
            Department.Medical => _medical,
            Department.Science => _science,
            Department.Cargo => _cargo,
            Department.Service => _service,
            Department.Civilian => _civilian,
            _ => Color.white,
        };

        private static Color Hex(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 0xff);
    }
}
