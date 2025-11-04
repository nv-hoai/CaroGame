using TMPro;
using UnityEngine;

public class ChatPanel : MonoBehaviour
{
    public GameObject messagePrefab;
    public TMP_InputField chatInputField;

    private void Start()
    {
        CanvasManager.Instance.panelDict["ChatPanel"] = this.gameObject;

        GameManager.Instance.Client.OnChatMessageReceived += (chatData) =>
        {
            GameObject messageObj = Instantiate(messagePrefab, transform);
            messageObj.GetComponent<TMP_Text>().text = $"{chatData.Sender}: {chatData.Message} {chatData.Timestamp}";
            //If there are over 3 messages in the chat, remove the oldest one
            if (transform.childCount > 3)
            {
                Destroy(transform.GetChild(0).gameObject);
            }
        };

        chatInputField.onSubmit.AddListener((string text) => SendMessage());
    }

    public void SendMessage()
    {
        string message = chatInputField.text;
        if (!string.IsNullOrEmpty(message))
        {
            _ = GameManager.Instance.Client.SendChat(message);
            chatInputField.text = string.Empty;
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }
}
