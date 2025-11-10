using System.Collections.Generic;
using UnityEngine;

public class RankPanel : MonoBehaviour
{
    public GameObject scrollContent;
    public GameObject rowPrefab;

    // 20 minutes cache for leaderboard data
    private List<LeaderboardEntry> leaderboardData;
    private float cacheDuration = 1200f;

    void Start()
    {
        CanvasManager.Instance.panelDict.Add("RankPanel", gameObject);

        GameManager.Instance.Client.OnLeaderboardReceived += (leaderboardData) =>
        {
            this.leaderboardData = leaderboardData;
            ShowRanking();
        };

        gameObject.SetActive(false);
    }

    private void Update()
    {
        // Refresh leaderboard data if cache is expired
        if (leaderboardData == null || cacheDuration <= 0.0f)
        {
            cacheDuration = 1200f;
            _ = GameManager.Instance.Client.GetLeaderboard();
        }

        cacheDuration -= Time.deltaTime;
    }

    private void ShowRanking()
    {
        foreach (Transform child in scrollContent.transform)
        {
            Destroy(child.gameObject);
        }

        // Populate leaderboard rows
        if (leaderboardData != null)
        {
            foreach (var entry in leaderboardData)
            {
                GameObject row = Instantiate(rowPrefab, scrollContent.transform);
                RankRow rankRow = row.GetComponent<RankRow>();
                rankRow.SetData(entry.Rank, entry.ProfileId, entry.AvatarUrl, entry.PlayerName, entry.Elo, entry.WinRate);
            }
        }
    }

    private void OnDestroy()
    {
        CanvasManager.Instance.panelDict.Remove("RankPanel");
    }
}
