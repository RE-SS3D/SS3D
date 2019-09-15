using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChatTab : MonoBehaviour, IDragHandler, IEndDragHandler, IBeginDragHandler
{
    [SerializeField]
    private TextMeshProUGUI Text;

    private Image image;

    public ChatTabData Data;

    private ChatWindow chatWindow;

    private Transform originParent;

    private void Awake()
    {
        image = GetComponent<Image>();
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

    public void OpenTab() => chatWindow.LoadTab(Data);


    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!Data.Removable) return;
        
        originParent = transform.parent;
        transform.SetParent(chatWindow.GetChatManager().transform);

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
            Destroy(gameObject);
        }
        else
        {
            chatWindow.GetChatManager().CreateChatWindow(Data, null, Input.mousePosition);
            Destroy(gameObject);
        }

        Text.raycastTarget = true;
        image.raycastTarget = true;

        if (originParent.childCount < 1) Destroy(chatWindow.gameObject);
    }
}