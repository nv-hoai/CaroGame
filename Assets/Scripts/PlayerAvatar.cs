using UnityEngine;
using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[CreateAssetMenu(fileName = "PlayerAvatar", menuName = "Scriptable Objects/PlayerAvatar")]
public class PlayerAvatar : ScriptableObject
{
    public Sprite avatarImg;
    public string playerName;
    public int playerElo;

    public Action OnAvatarChanged; 

    public void UpdateAvatar(string newImg, string newName, int newElo)
    {
        if (GameManager.Instance.avatarSprites.ContainsKey(newImg))
        {
            avatarImg = GameManager.Instance.avatarSprites[newImg];
        }

        playerName = newName;
        playerElo = newElo;
        OnAvatarChanged?.Invoke();
    }

    public void UpdateImg(string newImg)
    {
        if (GameManager.Instance.avatarSprites.ContainsKey(newImg))
        {
            avatarImg = GameManager.Instance.avatarSprites[newImg];
            OnAvatarChanged?.Invoke();
        }
    }

    public void UpdateName(string newName)
    {
        playerName = newName;
        OnAvatarChanged?.Invoke();
    }
}
