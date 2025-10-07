using TMPro;
using UnityEngine;

public class ThreeDotsAnimation : MonoBehaviour
{
    private TextMeshProUGUI threeDots;

    private void Start()
    {
        threeDots = GetComponent<TextMeshProUGUI>();
        InvokeRepeating("AnimateDots", 0f, 0.5f);
    }

    private void AnimateDots()
    {
        if (threeDots.text.Length >= 3)
        {
            threeDots.text = "";
        }
        else
        {
            threeDots.text += ".";
        }
    }
}
