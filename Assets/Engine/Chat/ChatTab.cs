using System.Collections;
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
            oldPos = transform.position;
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
            oldPos = transform.position;
            originParent = transform.parent;

            chatWindow.LoadTab();
        
            Text.raycastTarget = false;
            image.raycastTarget = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position += (Vector3) eventData.delta;
        }

        /// <summary>
        /// Places the tab close to where the mouse dropped the tab off at. Reorders the other tabs to adjust.
        /// </summary>
        /// <param name="tabTransform">The tab to compare positions with.</param>
        /// <param name="cw">The ChatWindow which contains the TabRow the tab goes into.</param>
        /// <param name="mousePos">The position of the mouse to compare which direction the tab will move to.</param>
        private void PlaceTab(ChatTab chatTab, ChatWindow cw, Vector3 mousePos)
        {
            RectTransform tabRow = cw.GetTabRow();
            int index = chatTab.transform.GetSiblingIndex();
            // If we are moving the tab to the right
            if (mousePos.x  > chatTab.oldPos.x)
            {
                // Figure out how far to the right
                for ( int i = 0; i < tabRow.childCount; i++)
                {
                    RectTransform child = (RectTransform) tabRow.GetChild(i);
                    float pos = child.position.x;
                    // consider old pos of tab since it is being held currently
                    if (child == (RectTransform)chatTab.transform)
                        pos = chatTab.oldPos.x;
                    
                    if (pos  + (child.rect.width / 2) < mousePos.x)
                        index = i;
                }
            }
            // Otherwise figure out how far left
            else
            {
                for ( int i = tabRow.childCount - 1; i >= 0; i--)
                {
                    RectTransform child = (RectTransform) tabRow.GetChild(i);
                    float pos = child.position.x;
                    // consider old pos of tab since it is being held currently
                    if (child == (RectTransform)chatTab.transform)
                        pos = chatTab.oldPos.x;
                    
                    if (pos  + (child.rect.width / 2) > mousePos.x)
                        index = i;
                }
            }
            
            // Put tab back where it was
            if (index == transform.GetSiblingIndex())
            {
                transform.position = chatTab.oldPos;
            }
            // Otherwise move tab over to new spot
            else
                transform.SetSiblingIndex(index);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.hovered.Any(x => x.GetComponentInParent<ChatWindow>()))
            {
                ChatWindow window = eventData.hovered.First(x => x.GetComponentInParent<ChatWindow>())
                    .GetComponentInParent<ChatWindow>(); // this line is ugly >:l
                
                // Check if dropping on the same window
                if (window == chatWindow)
                {
                    PlaceTab(this, chatWindow, eventData.position);
                    chatWindow.SelectTab(gameObject);
                }
                else
                {
                    ChatTab chatTab = window.AddTab(Data);
                    PlaceTab(chatTab, window, eventData.position);
                    // Delete the old chat window if it wouldn't have any tabs after deleting this one
                    if (chatWindow.GetTabCount() < 2)
                        chatWindow.ChatRegister.DeleteChatWindow(chatWindow);
                    Destroy(gameObject);
                    chatWindow.SelectNextTab(gameObject);
                }
            }
            else
            {
                // Create a new chat window as long as there are multiple tabs
                if (chatWindow.GetTabCount() > 1)
                {
                    chatWindow.ChatRegister.CreateChatWindow(Data, null, Input.mousePosition);
                    Destroy(gameObject);
                    chatWindow.SelectNextTab(gameObject);
                }
                // There aren't multiple tabs, just revert back to where you were before
                else
                    transform.position = oldPos;
            }
            Text.raycastTarget = true;
            image.raycastTarget = true;
        }
    }
}