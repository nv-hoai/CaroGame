using UnityEngine;

public class WaitingPanel : MonoBehaviour
{
    void Start()
    {
        CanvasManager.Instance.panelDict.Add("WaitingPanel", gameObject);

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        CanvasManager.Instance.panelDict.Remove("WaitingPanel");
    }
}
