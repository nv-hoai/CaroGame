using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Avatar : MonoBehaviour
{
    public PlayerAvatar playerAvatar;

    public Image image;
    public TextMeshProUGUI playerName;
    public TextMeshProUGUI playerElo;
    public int eventCase = 0;

    private void Start()
    {
        playerAvatar.OnAvatarChanged += UpdateAvatarUI;

        switch (eventCase)
        {
            case 0:
                GameManager.Instance.Client.OnProfileDataReceived += (data) =>
                {
                    playerAvatar.UpdateAvatar(data.AvatarUrl, data.PlayerName, data.Elo);
                };
                GameManager.Instance.Client.OnPlayerInfoReceived += (data) =>
                {
                    playerAvatar.UpdateAvatar(data.AvatarUrl, data.PlayerName, data.PlayerElo);
                };
                break;
            case 1:
                GameManager.Instance.Client.OnOpponentInfoReceived += (data) =>
                {
                    playerAvatar.UpdateAvatar(data.AvatarUrl, data.PlayerName, data.PlayerElo);
                };
                break;
            default:
                break;
        }
    }

    private void UpdateAvatarUI()
    {
        image.sprite = playerAvatar.avatarImg;
        playerName.text = playerAvatar.playerName;
        playerElo.text = "Elo: " + playerAvatar.playerElo.ToString();
        Debug.Log("Avatar UI updated.");
    }
}
