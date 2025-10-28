using UnityEngine;
using System.Collections;

public class GameCanvas : MonoBehaviour
{
    void Start()
    {
        CanvasManager.Instance.canvasDict.Add("GameCanvas", gameObject);

        StartCoroutine(CloseCanvas());
    }

    IEnumerator CloseCanvas()
    {
        yield return new WaitForEndOfFrame();
        CanvasManager.Instance.CloseCanvas("GameCanvas");
    }

    private void OnDestroy()
    {
        CanvasManager.Instance.canvasDict.Remove("GameCanvas");
    }
}
