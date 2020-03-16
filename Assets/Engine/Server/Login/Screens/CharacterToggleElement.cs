using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Engine.Server.Login.Screens
{
    /// <summary>
    /// MonoBehaviour responsible for registering listeners to the dynamically created character select and delete buttons.
    /// Should be attached to the Character Toggle Element prefab.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class CharacterToggleElement : MonoBehaviour
    {
        private int id;
        
        public int Id => id;
        public void Register(int id, LoginWindow loginWindow)
        {
            this.id = id;
            Toggle toggle = GetComponent<Toggle>();
            Button deleteButton = GetComponentInChildren<Button>();
            toggle.onValueChanged.AddListener(delegate
            {
                loginWindow.HandleCharacterToggle(id, toggle.isOn);
            });
            deleteButton.onClick.AddListener(delegate
            {
                loginWindow.HandleCharacterDeleteButton(id);
            });
            
        }
    }
}
