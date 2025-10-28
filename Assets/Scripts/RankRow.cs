using TMPro;
using UnityEngine;

public class RankRow : MonoBehaviour
{
    public TMP_Text rankText;
    public TMP_Text playerNameText;
    public TMP_Text eloText;
    public TMP_Text winrateText;

    public void SetData(int rank, string playerName, int elo, double winRate)
    {
        rankText.text = rank.ToString();
        playerNameText.text = playerName;
        eloText.text = elo.ToString();
        winrateText.text = $"{winRate/100:P1}";
    }
}
