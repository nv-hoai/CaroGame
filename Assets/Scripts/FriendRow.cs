using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendRow : MonoBehaviour
{
    public Image img;
    public TMP_Text idText;
    public TMP_Text playerNameText;
    public TMP_Text eloText;
    public TMP_Text statusText;

    public void SetData(int id, string avatarUrl, string playerName, int elo, string status="Online")
    {
        idText.text = id.ToString();
        img.sprite = GameManager.Instance.avatarSprites[avatarUrl];
        playerNameText.text = playerName;
        eloText.text = elo.ToString();

        if (status == "Online")
        {
            statusText.text = "<color=green>" + status + "</color>";
        }
        else
        {
            statusText.text = "<color=red>" + status + "</color>";
        }
    }
}
