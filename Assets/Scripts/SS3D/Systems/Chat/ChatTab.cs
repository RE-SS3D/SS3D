﻿using Coimbra;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SS3D.Engine.Chat
{
    public class ChatTab : MonoBehaviour, IDragHandler, IEndDragHandler, IBeginDragHandler
    {
        [FormerlySerializedAs("chatWindowPrefab")]
        [SerializeField] private InGameChatWindow inGameChatWindowPrefab = null;
        [SerializeField] private TextMeshProUGUI text = null;
        [SerializeField] private ChatTabData data;
        
        private Image _image;
        private InGameChatWindow _inGameChatWindow;
        private Vector3 _oldPos;

        public ChatTabData GetChatTabData() => data;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        public void Init(ChatTabData newData, InGameChatWindow window)
        {
            data = newData;
            _inGameChatWindow = window;
            text.text = newData.name;
            transform.SetAsFirstSibling();
            data.tab = this;

            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(FixTabWidth());
            }

            _oldPos = transform.position;
        }

        private IEnumerator FixTabWidth()
        {
            yield return null;
            
            ((RectTransform)transform).SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal, 20 + text.GetRenderedValues(true).x);
        }

        public void OpenTab()
        {
            _inGameChatWindow.SelectTab(gameObject);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _oldPos = transform.position;
            _inGameChatWindow.LoadTab();
        
            text.raycastTarget = false;
            _image.raycastTarget = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position += (Vector3)eventData.delta;
        }

        /// <summary>
        /// Places the tab close to where the mouse dropped the tab off at. Reorders the other tabs to adjust.
        /// </summary>
        /// <param name="chatTab">The tab to compare positions with.</param>
        /// <param name="inGameChatWindow">The ChatWindow which contains the TabRow the tab goes into.</param>
        /// <param name="mousePos">The position of the mouse to compare which direction the tab will move to.</param>
        private void PlaceTab(ChatTab chatTab, InGameChatWindow inGameChatWindow, Vector3 mousePos)
        {
            RectTransform tabRow = inGameChatWindow.GetTabRow();
            int index = chatTab.transform.GetSiblingIndex();
            
            // If we are moving the tab to the right
            if (mousePos.x  > chatTab._oldPos.x)
            {
                // Figure out how far to the right
                for (int i = 0; i < tabRow.childCount; i++)
                {
                    RectTransform child = (RectTransform) tabRow.GetChild(i);
                    float pos = child.position.x;
                    // consider old pos of tab since it is being held currently
                    if (child == (RectTransform)chatTab.transform)
                    {
                        pos = chatTab._oldPos.x;
                    }

                    if (pos + (child.rect.width / 2) < mousePos.x)
                    {
                        index = i;
                    }
                }
            }
            // Otherwise figure out how far left
            else
            {
                for (int i = tabRow.childCount - 1; i >= 0; i--)
                {
                    RectTransform child = (RectTransform) tabRow.GetChild(i);
                    float pos = child.position.x;
                    // consider old pos of tab since it is being held currently
                    if (child == (RectTransform)chatTab.transform)
                    {
                        pos = chatTab._oldPos.x;
                    }

                    if (pos + (child.rect.width / 2) > mousePos.x)
                    {
                        index = i;
                    }
                }
            }
            
            // Put tab back where it was
            if (index == transform.GetSiblingIndex())
            {
                transform.position = chatTab._oldPos;
            }
            // Otherwise move tab over to new spot
            else
            {
                transform.SetSiblingIndex(index);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            InGameChatWindow inGameChatWindow = eventData.hovered
                .FirstOrDefault(x => x.GetComponentInParent<InGameChatWindow>() != null)?
                .GetComponentInParent<InGameChatWindow>();
            if (inGameChatWindow != null)
            {
                // Check if dropping on the same window
                if (inGameChatWindow == _inGameChatWindow)
                {
                    PlaceTab(this, _inGameChatWindow, eventData.position);
                    _inGameChatWindow.SelectTab(gameObject);
                }
                else
                {
                    ChatTab chatTab = inGameChatWindow.AddTab(data);
                    PlaceTab(chatTab, inGameChatWindow, eventData.position);
                    Button a = _inGameChatWindow.GetNextTabButton(gameObject);
                    gameObject.Dispose(false);

                    if (a == null)
                    {
                        _inGameChatWindow.gameObject.Dispose(false);
                    }
                    else
                    {
                        _inGameChatWindow.SelectTab(a.gameObject);
                    }

                    return;
                }
            }
            else
            {
                // Create a new chat window as long as there are multiple tabs
                if (_inGameChatWindow.GetTabCount() > 1)
                {
                    InGameChatWindow newInGameChatWindow = Instantiate(inGameChatWindowPrefab).GetComponent<InGameChatWindow>();
                    newInGameChatWindow.transform.SetParent(_inGameChatWindow.transform.parent);
                    newInGameChatWindow.transform.position = Input.mousePosition;
                    newInGameChatWindow.transform.localScale = _inGameChatWindow.transform.localScale;
                    newInGameChatWindow.gameObject.SetActive(true);
                    newInGameChatWindow.AddTab(data);
                    Button a = _inGameChatWindow.GetNextTabButton(gameObject);
                    gameObject.Dispose(false);
                    _inGameChatWindow.SelectTab(a.gameObject);

                    return;
                }
                // There aren't multiple tabs, just revert back to where you were before
                else
                {
                    transform.position = _oldPos;
                }
            }
            
            text.raycastTarget = true;
            _image.raycastTarget = true;
        }
    }
}