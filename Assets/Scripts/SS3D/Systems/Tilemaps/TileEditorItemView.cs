using SS3D.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Systems.Tilemaps
{
    public class TileEditorItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private TileObjects _tileObject;

        [SerializeField] private Button _button;

        public Button.ButtonClickedEvent OnPressed => _button.onClick;

        public void SetupLabel(TileObjects tileObject)
        {
            _tileObject = tileObject;

            _label.SetText(_tileObject.ToString());
        }
    }
}
