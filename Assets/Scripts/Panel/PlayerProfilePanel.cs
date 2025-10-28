using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class PlayerProfilePanel : MonoBehaviour
{
    public GameObject iconScrollContent;
    public GameObject iconPrefab;
    
    private void Start()
    {
        CanvasManager.Instance.panelDict.Add("PlayerProfilePanel", gameObject);

        InitAvatar();

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
