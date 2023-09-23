using SS3D.Attributes;
using TMPro;
using UnityEngine;

namespace SS3D.Systems.Lobby.UI
{
    /// <summary>
    /// Simple Username ui element controller
    /// </summary>
    [RequiredLayer("UI")]
    public sealed class PlayerUsernameView : MonoBehaviour
    {
        [SerializeField]
        [NotNull]
        private TMP_Text _nameLabel;

        public string Name => _nameLabel.text;

        public void UpdateNameText(string newName)
        {
            _nameLabel.text = newName;
        }

        public void UpdateNameColor(Color color)
        {
            _nameLabel.color = color;
        }
    }
}
