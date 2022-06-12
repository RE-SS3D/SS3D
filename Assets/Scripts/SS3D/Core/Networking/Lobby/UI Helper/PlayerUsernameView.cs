using TMPro;
using UnityEngine;

namespace SS3D.Core.Networking.Lobby.UI_Helper
{
    /// <summary>
    /// Simple Username ui element controller
    /// </summary>
    public sealed class PlayerUsernameView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameLabel;

        public string Name => _nameLabel.text;
    
        public void UpdateNameText(string newName)
        {
            _nameLabel.text = newName;
        }
    }
}
