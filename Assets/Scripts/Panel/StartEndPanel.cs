using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class StartEndPanel : MonoBehaviour
{
    public TextMeshProUGUI title, result;
    public GameObject homeButton1, homeButton2;
    public Board board;
    public GameObject img;

    private void Start()
    {
        CanvasManager.Instance.panelDict.Add("StartEndPanel", gameObject);

        GameManager.Instance.Client.OnGameStarted += ((roomId, currentPlayer) => GameStart(roomId, currentPlayer));
        GameManager.Instance.Client.OnGameEnded += ((reason, winner) => GameEnd(reason, winner));

        StartCoroutine(Hide());
    }

    public void GameStart(string roomId, string currentPlayer)
    {
        if (board != null)
            board.ResetBoard();

        title.text = "Match Start";
        result.text = string.Empty;

        img.SetActive(false);
        homeButton1.SetActive(false);
        homeButton2.SetActive(true);

        CanvasManager.Instance.OpenPanel("StartEndPanel");

        StartCoroutine(HidePanel());
    }

    IEnumerator Hide()
    {
        yield return new WaitForEndOfFrame();
        gameObject.SetActive(false);
    }

    IEnumerator HidePanel()
    {
        yield return new WaitForSeconds(1.5f);

        CanvasManager.Instance.CloseCanvas("ForeCanvas");
        CanvasManager.Instance.OpenCanvas("GameCanvas");
        CanvasManager.Instance.ClosePanel("StartEndPanel");
    }

    public void GameEnd(string reason, string winner)
    {
        string winnerName;
        title.text = "Game Ended";
        if (winner == "NONE")
            winnerName = "No one";
        else if (winner == GameManager.Instance.Client.MyPlayerSymbol)
            winnerName = GameManager.Instance.Client.MyPlayerInfo.PlayerName;
        else
            winnerName = GameManager.Instance.Client.OpponentInfo.PlayerName;
        result.text ="Winner: " + winnerName;
        
        homeButton1.SetActive(true);

        CanvasManager.Instance.OpenPanel("StartEndPanel");
        _ = GameManager.Instance.Client.GetProfile();
    }

    private void OnDestroy()
    {
        CanvasManager.Instance.panelDict.Remove("StartEndPanel");
    }
}
