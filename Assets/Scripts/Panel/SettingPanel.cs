using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : MonoBehaviour
{
    public Transform buttonContainer;

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void BackHome()
    {
        _ = GameManager.Instance.Client.LeaveMatch();
        CanvasManager.Instance.SwitchCanvas(1);
    }

    private void OnEnable()
    {
        if (buttonContainer == null)
        {
            Debug.LogWarning("Button container is not assigned.");
            return;
        }

        if (CanvasManager.Instance.currentCanvasIndex == 1)
        {
            buttonContainer.gameObject.SetActive(false);
        }
        else
        {
            buttonContainer.gameObject.SetActive(true);
        }
    }
}
