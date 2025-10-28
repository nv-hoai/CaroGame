using TMPro;
using UnityEngine;

public class HistoryRow : MonoBehaviour
{
    public TMP_Text idText;
    public TMP_Text matchText;
    public TMP_Text resultText;
    public TMP_Text eloText;

    public void SetData(int id, string match, string result, string winner, int elo)
    {
        idText.text = id.ToString();
        matchText.text = match;

        if (result == "Draw")
        {
            resultText.text = "<color=yellow>DRAW</color>";
        }
        else if (winner == GameManager.Instance.Client.MyPlayerInfo.PlayerName)
        {
            resultText.text = "<color=green>WIN</color>";
        }
        else
        {
            resultText.text = "<color=red>LOSE</color>";
        }

        if (elo >= 0)
        {
            eloText.text = "<color=green>+" + elo.ToString() + "</color>";
        }
        else
        {
            eloText.text = "<color=red>" + elo.ToString() + "</color>";
        }
    }
}
