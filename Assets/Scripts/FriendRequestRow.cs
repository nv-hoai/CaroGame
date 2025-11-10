using System;
using System.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendRequestRow : MonoBehaviour
{
    public Image img;
    public TMP_Text id;
    public TMP_Text playerName;
    public TMP_Text timeRequested;

    public int friendShipId;

    public void SetData(int friendShipId, int profileId, string avatarUrl, string playerName, string dateTime)
    {
        this.friendShipId = friendShipId;
        id.text = profileId.ToString();
        img.sprite = GameManager.Instance.avatarSprites[avatarUrl];
        this.playerName.text = playerName;
        timeRequested.text = DateTime.Parse(dateTime).ToString("g");
    }

    public void OnAcceptButtonClicked()
    {
        _ = GameManager.Instance.Client.AcceptFriendRequest(friendShipId);
        Destroy(gameObject);
    }

    public void OnRejectButtonClicked()
    {
        _ = GameManager.Instance.Client.RejectFriendRequest(friendShipId);
        Destroy(gameObject);
    }
}
