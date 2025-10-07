using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Avatar : MonoBehaviour
{
    public Image image;
    public TextMeshProUGUI playerName;
    public TextMeshProUGUI playerElo;
    
    public void SetImage(Sprite sprite)
    {
        image.sprite = sprite;
    }

    public void SetPlayerName(string name)
    {
        playerName.text = name;
    }

    public void SetPlayerElo(int elo)
    {
        playerElo.text = elo.ToString();
    }
}
