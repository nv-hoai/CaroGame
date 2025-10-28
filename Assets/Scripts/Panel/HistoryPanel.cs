using UnityEngine;
using System.Collections.Generic;

public class HistoryPanel : MonoBehaviour
{
    public GameObject rowPrefab;
    public GameObject scrollContent;

    private List<GameHistoryEntry> historyData;

    private void Start()
    {
        CanvasManager.Instance.panelDict.Add("HistoryPanel", gameObject);

        GameManager.Instance.Client.OnGameHistoryReceived += (data) =>
        {
            historyData = data;
            ShowHistory();
        };

        gameObject.SetActive(false);
    }

    private void ShowHistory()
    {
        foreach (Transform child in scrollContent.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (var entry in historyData)
        {
            GameObject row = Instantiate(rowPrefab, scrollContent.transform);
            HistoryRow historyRow = row.GetComponent<HistoryRow>();
            historyRow.SetData(entry.GameId, entry.Player1Name + " VS " + entry.Player2Name, 
                        entry.GameResult, entry.WinnerName, entry.EloChange);
        }
    }

    private void OnEnable()
    {
        _ = GameManager.Instance.Client.GetGameHistory();
    }

    private void OnDestroy()
    {
        CanvasManager.Instance.panelDict.Remove("HistoryPanel");
    }
}
