using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RankRow : MonoBehaviour
{
    public Image img;
    public TMP_Text rankText;
    public TMP_Text playerNameText;
    public TMP_Text eloText;
    public TMP_Text winrateText;

    public int profileId;

    public void SetData(int rank, int profileId, string avatarUrl, string playerName, int elo, double winRate)
    {
        rankText.text = rank.ToString();
        img.sprite = GameManager.Instance.avatarSprites[avatarUrl];
        playerNameText.text = playerName;
        eloText.text = elo.ToString();
        winrateText.text = $"{winRate/100:P1}";
        this.profileId = profileId;
    }
}
