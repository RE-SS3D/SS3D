using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace SS3D.Content.Systems.Examine
{
    public class ExamineUI : MonoBehaviour
    {
        public Transform Panel;
        public TMP_Text Text;

        public void Start()
        {
            Assert.IsNotNull(Panel);
            Assert.IsNotNull(Text);
        }
        
        public void SetText(string text)
        {
            Text.text = text;
        }

        public void SetPosition(Vector2 position)
        {
            Panel.position = new Vector3(position.x, position.y, 0);
        }
    }
}
