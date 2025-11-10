using UnityEngine;
using TMPro;

public class TrackPanel : MonoBehaviour
{
    public GameObject trackPrefab;

    private void Start()
    {
        CanvasManager.Instance.panelDict["TrackPanel"] = this.gameObject;

        GameManager.Instance.Client.OnMoveReceived += (move) =>
        {
            GameObject trackObj = Instantiate(trackPrefab, transform);
            trackObj.GetComponent<TMP_Text>().text = $"Row: {move.row}, Column: {('A' + move.col).ToString()}";
            if (transform.childCount > 15)
            {
                Destroy(transform.GetChild(0).gameObject);
            }
        };
    }

    private void OnDisable()
    {
        //Clear all children
        for (int i=0; i<transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }
}
