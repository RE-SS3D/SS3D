﻿using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SS3D.Engine.Chat
{
    public class ChatTab : MonoBehaviour, IDragHandler, IEndDragHandler, IBeginDragHandler
    {
        [SerializeField] private TextMeshProUGUI Text = null;

        private Image image;
        public ChatTabData Data;
        private ChatWindow chatWindow;
        private Transform originParent;
        private Vector3 oldPos;

        private void Awake()
        {
            image = GetComponent<Image>();
        }

        public ChatTabData GetChatTabData()
        {
            return Data;
        }

        public void Init(ChatTabData data, ChatWindow window)
        {
            Data = data;
            chatWindow = window;
            Text.text = data.Name;
            transform.SetAsFirstSibling();
            Data.Tab = this;
            StartCoroutine(FixTabWidth());
        }

        private IEnumerator FixTabWidth()
        {
            yield return null;
            ((RectTransform) transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                20 + Text.GetRenderedValues(true).x);
        }

        public void OpenTab()
        {
            chatWindow.SelectTab(gameObject);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!Data.Removable) return;
            oldPos = transform.position;
            originParent = transform.parent;

            chatWindow.LoadTab();
        
            Text.raycastTarget = false;
            image.raycastTarget = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!Data.Removable) return;
            transform.position += (Vector3) eventData.delta;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!Data.Removable) return;

            if (eventData.hovered.Any(x => x.GetComponentInParent<ChatWindow>()))
            {
                ChatWindow window = eventData.hovered.First(x => x.GetComponentInParent<ChatWindow>())
                    .GetComponentInParent<ChatWindow>(); // this line is ugly >:l
                window.AddTab(Data);
                if (chatWindow.GetTabCount() < 2)
                    Destroy(chatWindow.gameObject);
                Destroy(gameObject);
            }
            else
            {
                // Create a new chat window as long as there are multiple tabs
                if (chatWindow.GetTabCount() > 1)
                {
                    chatWindow.ChatRegister.CreateChatWindow(Data, null, Input.mousePosition);
                    Destroy(gameObject);
                }
                // There aren't multiple tabs, just revert back to where you were before
                else
                    transform.position = oldPos;
            }
            chatWindow.SelectNextTab(gameObject);
            Text.raycastTarget = true;
            image.raycastTarget = true;
        }
    }
}