using UnityEngine;
using UnityEngine.Assertions;

namespace SS3D.Content.Graphics.UI
{
    public class ListMenu : Menu
    {
        public Transform list;

        private void Start()
        {
            Assert.IsNotNull(list);
        }

        public void AddElement(GameObject element)
        {
            element.transform.SetParent(list, false);
        }

        public void RemoveElement(GameObject element)
        {
            element.transform.parent = null;
        }
    }
}
