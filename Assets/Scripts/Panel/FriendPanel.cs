using UnityEngine;
using System.Collections.Generic;

public class FriendPanel : MonoBehaviour
{
    public GameObject scrollContent;
    public GameObject rowPrefab;

    private List<FriendData> friendList;

    void Start()
    {
        CanvasManager.Instance.panelDict.Add("FriendPanel", gameObject);

        GameManager.Instance.Client.OnFriendsListReceived += (friends) =>
        {
            friendList = friends;
            ShowFriends();
        };

        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        _ = GameManager.Instance.Client.GetFriends();
    }

    private void ShowFriends()
    {
        foreach (Transform child in scrollContent.transform)
        {
            Destroy(child.gameObject);
        }

        if (friendList != null)
        {
            foreach (var friend in friendList)
            {
                GameObject row = Instantiate(rowPrefab, scrollContent.transform);
                FriendRow friendRow = row.GetComponent<FriendRow>();
                friendRow.SetData(friend.ProfileId, friend.PlayerName, friend.Elo);
            }
        }
    }

    private void OnDestroy()
    {
        CanvasManager.Instance.panelDict.Remove("FriendPanel");
    }
}
