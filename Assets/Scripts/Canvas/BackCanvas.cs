using UnityEngine;

public class BackCanvas : MonoBehaviour
{
    void Start()
    {
        CanvasManager.Instance.canvasDict.Add("BackCanvas", gameObject);
    }

    private void OnDestroy()
    {
        CanvasManager.Instance.canvasDict.Remove("BackCanvas");
    }
}
