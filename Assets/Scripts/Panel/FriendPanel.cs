using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class FriendPanel : MonoBehaviour
{
    public GameObject scrollFriendsContent;
    public GameObject scrollRequestsContent;
    public GameObject scrollSearchContent;
    public GameObject scrollFriends;
    public GameObject scrollRequests;
    public GameObject scrollSearch;
    public GameObject searchPrefab;
    public GameObject friendPrefab;
    public GameObject requestPrefab;
    public TMP_InputField inputField;

    private List<FriendData> friendList;

    void Start()
    {
        CanvasManager.Instance.panelDict.Add("FriendPanel", gameObject);

        GameManager.Instance.Client.OnFriendsListReceived += (friends) =>
        {
            friendList = friends;
            ShowFriends();
        };

        GameManager.Instance.Client.OnFriendRequestsReceived += 
            (requests) => ShowFriendRequests(requests);

        GameManager.Instance.Client.OnPlayerSearchResultsReceived += 
            (profiles) => ShowSearchResults(profiles);

        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        _ = GameManager.Instance.Client.GetFriends();
        _ = GameManager.Instance.Client.GetFriendRequests();
        OnFriendListClick();
    }

    private void ShowFriends()
    {
        foreach (Transform child in scrollFriendsContent.transform)
        {
            Destroy(child.gameObject);
        }

        if (friendList != null)
        {
            foreach (var friend in friendList)
            {
                GameObject row = Instantiate(friendPrefab, scrollFriendsContent.transform);
                FriendRow friendRow = row.GetComponent<FriendRow>();
                friendRow.SetData(friend.ProfileId, friend.AvatarUrl, friend.PlayerName, friend.Elo, friend.Status);
            }
        }
    }

    private void ShowFriendRequests(List<FriendRequestData> friendRequestList)
    {
        foreach (Transform child in scrollRequestsContent.transform)
        {
            Destroy(child.gameObject);
        }
        if (friendRequestList != null)
        {
            foreach (var request in friendRequestList)
            {
                GameObject row = Instantiate(requestPrefab, scrollRequestsContent.transform);
                FriendRequestRow requestRow = row.GetComponent<FriendRequestRow>();
                requestRow.SetData(request.FriendshipId, request.ProfileId, request.AvatarUrl, request.PlayerName, request.RequestedAt);
            }
        }
    }

    private void ShowSearchResults(List<ProfileData> profiles)
    {
        foreach (Transform child in scrollSearchContent.transform)
        {
            Destroy(child.gameObject);
        }
        foreach (var player in profiles)
        {
            GameObject row = Instantiate(searchPrefab, scrollSearchContent.transform);
            RankRow rankRow = row.GetComponent<RankRow>();
            rankRow.SetData(player.ProfileId, player.ProfileId, player.AvatarUrl, player.PlayerName, player.Elo, player.WinRate);
        }
    }

    public void OnFriendListClick()
    {
        scrollFriends.SetActive(true);
        scrollRequests.SetActive(false);
        scrollSearch.SetActive(false);
    }

    public void OnRequestListClick()
    {
        scrollFriends.SetActive(false);
        scrollRequests.SetActive(true);
        scrollSearch.SetActive(false);
    }

    public void OnSearchClick()
    {
        if (inputField.text.Length <= 0) return;
        
        scrollFriends.SetActive(false);
        scrollRequests.SetActive(false);
        scrollSearch.SetActive(true);

        _ = GameManager.Instance.Client.SearchPlayerByName(inputField.text);
    }

    private void OnDestroy()
    {
        CanvasManager.Instance.panelDict.Remove("FriendPanel");
    }
}
