using System.Collections;
using TMPro;
using UnityEngine;

public class StartEndPanel : MonoBehaviour
{
    public TextMeshProUGUI title, result;
    public Avatar p1, p2;
    public Transform homeButton;

    private void Start()
    {
        GameManager.Instance.Client.OnGameStarted += ((roomId, currentPlayer) => GameStart(roomId, currentPlayer));
        GameManager.Instance.Client.OnGameEnded += ((reason, winner) => GameEnd(reason, winner));

        gameObject.SetActive(false);
    }

    public void GameStart(string roomId, string currentPlayer)
    {
        title.text = "Match Start";
        result.text = string.Empty;
        p1.SetPlayerName(GameManager.Instance.Client.MyPlayerInfo.playerName);
        p2.SetPlayerName(GameManager.Instance.Client.OpponentInfo.playerName);
        homeButton.gameObject.SetActive(false);
        CanvasManager.Instance.OpenPanel(6);
        CanvasManager.Instance.DisableClose();
        StartCoroutine(HidePanel());
    }

    IEnumerator HidePanel()
    {
        yield return new WaitForSeconds(1.5f);
        CanvasManager.Instance.SwitchCanvas(2);
        CanvasManager.Instance.CloseCurrentPanel();
    }

    public void GameEnd(string reason, string winner)
    {
        title.text = "Game Ended";
        string winnerName;
        if (winner == "NONE")
        {
            winnerName = "No one";
        }
        else if (winner == GameManager.Instance.Client.MyPlayerSymbol)
        {
            winnerName = GameManager.Instance.Client.MyPlayerInfo.playerName;
        }
        else
        {
            winnerName = GameManager.Instance.Client.OpponentInfo.playerName;
        }
        result.text ="Winner: " + winnerName;
        homeButton.gameObject.SetActive(true);
        CanvasManager.Instance.OpenPanel(6);
    }
}
