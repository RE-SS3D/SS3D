using Coimbra;
using UnityEngine;

namespace SS3D.Interactions.UI.RadialMenuInteraction
{
    public class PetalsManager : MonoBehaviour
    {
        public GameObject PetalPrefab;
        public GameObject PetalParent;
        public RadialInteractionMenuView ContextMenu;

        private PetalFolder _folder;
        private int _petalIndex = 0;

        private static readonly int AnimationReturnButtonVisible = Animator.StringToHash("ReturnButtonVisible");

        public void Update()
        {
            if (_folder == null || _folder.IsDirty != true || !_folder.CheckAnimationDone())
            {
                return;
            }

            UpdatePetals();
            _folder.IsDirty = false;
        }

        private void UpdatePetals()
        {
            int i = 0;

            foreach (Petal petal in _folder.Petals)
            {
                // if is active
                if (i >= _petalIndex && i < _petalIndex + 8) 
                {
                    // if was active
                    if (petal.IsVisible())
                    {
                        int diff = ((int)(petal.transform.localEulerAngles.z + 0.5f) - ((i - _petalIndex) * -45)) % 360;
                        switch (diff)
                        {
                            case > 180:
                                petal.Rotate(false);
                                break;
                            case < 180 when diff != 0:
                                petal.Rotate(true);
                                break;
                        }
                    }
                    else
                    {
                        petal.gameObject.SetActive(true);
                        petal.Appear();
                    }
                    petal.transform.localEulerAngles = new Vector3(0, 0, (360 + ((i - _petalIndex) * -45)) % 360);
                }
                else
                {
                    // if was active but not anymore
                    if (petal.IsVisible()) 
                    {
                        petal.Disappear();
                    }
                }
                i++;
            }
            if (ContextMenu != null)
            {
                ContextMenu.Animator.SetBool(AnimationReturnButtonVisible, this._folder.Folder != null);
            }
        }

        public PetalFolder GetFolder()
        {
            return _folder;
        }

        public void SetFolder(PetalFolder newFolder, bool isRoot)
        {
            _petalIndex = 0;
            if (isRoot && _folder != null)
            {
                _folder.Clear();
            }
            else if (_folder != null)
            {
                _folder.Disable();
            }

            newFolder.Folder = (isRoot ? null : _folder);
            _folder = newFolder;
            _folder.IsDirty = true;
        }

        public void Return()
        {
            _petalIndex = 0;
            if (_folder.Folder == null)
            {
                return ;
            }

            _folder.Clear();
            _folder = _folder.Folder;
            _folder.Enable();
            _folder.IsDirty = true;
        }

        public Petal AddPetalToFolder()
        {
            Petal newPetal = Instantiate(PetalPrefab, PetalParent.transform).GetComponent<Petal>();
            newPetal.transform.parent = PetalParent.transform;
            _folder.AddPetal(newPetal);
            _folder.IsDirty = true;
            return newPetal;
        }
        public Petal AddPetalToFolder(Sprite icon, string name)
        {
            Petal newPetal = Instantiate(PetalPrefab, PetalParent.transform).GetComponent<Petal>();
            newPetal.iconImage.texture = icon.texture;
            newPetal.name = name;
            newPetal.transform.SetParent(PetalParent.transform, false);
            _folder.AddPetal(newPetal);
            _folder.IsDirty = true;
            return (newPetal);
        }

        public void Clear()
        {
            foreach(Petal petal in _folder.Petals)
            {
                petal.Destroy();
            }

            _folder.Petals.Clear();
            _folder.IsDirty = true;
        }

        public void MoveIndex(int offset)
        {
            if (_folder == null)
            {
                return;
            }

            _petalIndex += offset;
            _folder.IsDirty = true;

            if (_petalIndex < 0 || _folder.Petals.Count <= 8)
            {
                _petalIndex = 0;
            }
            else if (_petalIndex > _folder.Petals.Count - 8)
            {
                _petalIndex = _folder.Petals.Count - 8;
            }
        }
    }
}