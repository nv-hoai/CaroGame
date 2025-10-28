using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AvatarIcon : MonoBehaviour, IPointerClickHandler
{
    public PlayerAvatar avatar;
    public Image iconImage;
    private string spriteName;

    private void Start()
    {
        GameManager.Instance.Client.OnUpdateAvatarSucess += (newAvatarName) =>
        {
            avatar.UpdateImg(newAvatarName);
        };
    }

    public void SetIcon(Sprite sprite)
    {
        spriteName = sprite.name;
        iconImage.sprite = sprite;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _ = GameManager.Instance.Client.UpdateAvatar(spriteName);
    }
}
