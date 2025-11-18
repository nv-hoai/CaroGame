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
            int col = 'A' + move.col;
            char colChar = (char)col;
            string currentPlayerSymbol = string.Empty;
            if (GameManager.Instance.Client.IsMyTurn)
            {
                if (GameManager.Instance.Client.MyPlayerSymbol == "X")
                    currentPlayerSymbol = "O";
                else
                    currentPlayerSymbol = "X";
            }
            else
            {
                currentPlayerSymbol = GameManager.Instance.Client.MyPlayerSymbol;
            }
            trackObj.GetComponent<TMP_Text>().text = $"Row: {move.row}, Column: {colChar}, {currentPlayerSymbol}";
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
