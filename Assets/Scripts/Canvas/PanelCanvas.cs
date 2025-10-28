using UnityEngine;

public class PanelCanvas : MonoBehaviour
{
    void Start()
    {
        CanvasManager.Instance.canvasDict.Add("PanelCanvas", gameObject);
    }

    private void OnDestroy()
    {
        CanvasManager.Instance.canvasDict.Remove("PanelCanvas");
    }
}
