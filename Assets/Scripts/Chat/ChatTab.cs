using TMPro;
using UnityEngine;

public class ChatTab : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI Text;

    public ChatTabData Data;

    public void UpdateText(string text)
    {
        Text.text = text;
    }
}