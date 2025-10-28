using TMPro;
using UnityEngine;
using System.Collections;
using System.Threading.Tasks;

public class FindMatchPanel : MonoBehaviour
{
    public TextMeshProUGUI title;

    private void Start()
    {
        CanvasManager.Instance.panelDict.Add("FindMatchPanel", gameObject);
        
        GameManager.Instance.Client.OnMatchFound += ((roomId) => MatchFound(roomId));
        
        gameObject.SetActive(false);
    }

    public void MatchFound(string roomId)
    {
        StartCoroutine(ReadyToStart());
    }

    private void OnEnable()
    {
        title.text = "Find match";
    }

    IEnumerator ReadyToStart()
    {
        title.text = "Match found";
        yield return new WaitForSeconds(1);
        title.text = "Get ready";
        yield return new WaitForSeconds(1);
        _ = GameManager.Instance.Client.SendMessage("START_GAME");
        yield return new WaitUntil(() => GameManager.Instance.Client.BothReady);
        CanvasManager.Instance.ClosePanel("FindMatchPanel");
    }

    private void OnDestroy()
    {
        CanvasManager.Instance.panelDict.Remove("FindMatchPanel");
    }
}
