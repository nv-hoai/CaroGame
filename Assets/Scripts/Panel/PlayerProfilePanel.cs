using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class PlayerProfilePanel : MonoBehaviour
{
    public GameObject iconScrollContent;
    public GameObject iconPrefab;
    public TMP_Text id, tg, winrate;
    
    private void Start()
    {
        CanvasManager.Instance.panelDict.Add("PlayerProfilePanel", gameObject);

        InitAvatar();

        GameManager.Instance.Client.OnProfileDataReceived += (playerData) =>
        {
            id.text = "Id: " + playerData.ProfileId;
            tg.text = "Total games: " + playerData.TotalGames.ToString();
            winrate.text = $"Winrate: {playerData.WinRate / 100:P1}";
        };

        gameObject.SetActive(false);
    }

    private void InitAvatar()
    {
        foreach (var sprite in GameManager.Instance.avatarSprites.Values)
        {
            GameObject iconGameObject = Instantiate(iconPrefab, iconScrollContent.transform);
            AvatarIcon icon = iconGameObject.GetComponent<AvatarIcon>();
            icon.SetIcon(sprite);
        }
    }

    private void OnEnable()
    {
        _ = GameManager.Instance.Client.GetProfile();
    }

    private void OnDestroy()
    {
        CanvasManager.Instance.panelDict.Remove("PlayerProfilePanel");
    }
}
