using UnityEngine;
using UnityEngine.UI;

public class Cell : MonoBehaviour
{
    private Image cellImage;

    private void Start()
    {
        cellImage = GetComponent<Image>();
    }

    public void SetCell(Sprite sprite)
    {
        cellImage.sprite = sprite;
    }
}
