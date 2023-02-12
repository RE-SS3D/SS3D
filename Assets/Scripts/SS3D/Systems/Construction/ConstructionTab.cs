using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Systems.Construction.UI
{
    public class ConstructionTab : MonoBehaviour
    {
        public Image _image;
        public Button _button;


        private TileObjectSo _tileObjectSo;
        private Sprite _icon;
        

        public void Setup(TileObjectSo tileObjectSo)
        {
            _tileObjectSo = tileObjectSo;
            _icon = tileObjectSo.icon;

            _image = GetComponent<Image>();
            _image.sprite = tileObjectSo.icon;
            transform.localScale = Vector3.one;

            GetComponentInChildren<TMP_Text>().text = tileObjectSo.nameString;
        }
    }
}