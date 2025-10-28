using UnityEngine;

public class ForeCanvas : MonoBehaviour
{
    void Start()
    {
        CanvasManager.Instance.canvasDict.Add("ForeCanvas", gameObject);
    }

    private void OnDestroy()
    {
        CanvasManager.Instance.canvasDict.Remove("ForeCanvas");
    }
}
