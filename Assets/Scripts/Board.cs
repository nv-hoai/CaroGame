using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Board : MonoBehaviour
{
    public int boardSize = 15;
    public GameObject cellPrefab;
    public GameObject labelPrefab;
    public Sprite blankSprite;
    public Sprite xSprite;
    public Sprite oSprite;

    private Cell[,] cells;
    private GridLayoutGroup grid;

    void Start()
    {
        grid = gameObject.GetComponent<GridLayoutGroup>();
        cells = new Cell[boardSize, boardSize];
        GameManager.Instance.board = this;
        CreateBoard();
    }

    public void CreateBoard()
    {
        grid.constraintCount = boardSize+2;
        for (int x = 0; x < boardSize+2; x++)
        {
            for (int y = 0; y < boardSize+2; y++)
            {
                int ix, iy;
                ix = x-1; iy = y-1;

                if (x == 0 || y == 0 || x == boardSize + 1 || y == boardSize + 1)
                {
                    GameObject label = Instantiate(labelPrefab, gameObject.transform);
                    label.GetComponent<TMP_Text>().text = string.Empty;
                    if (x == 0 && (y > 0 && y < boardSize + 1 ))
                    {
                        label.GetComponent<TMP_Text>().text = ((char)('A' + iy)).ToString();
                    }
                    if (y == 0 && (x > 0 && x < boardSize + 1))
                    {
                        label.GetComponent<TMP_Text>().text = (ix).ToString();
                    }
                    continue;
                }

                GameObject cellObject = Instantiate(cellPrefab, gameObject.transform);
                Cell cell = cellObject.GetComponent<Cell>();
                Button btn = cell.GetComponent<Button>();
                
                cells[ix, iy] = cell;
                btn.onClick.AddListener(() => OnCellClick(ix, iy));
            }
        }
    }

    public void ResetBoard()
    {
        foreach (Cell cell in cells)
            cell.SetCell(blankSprite);
    }

    void OnCellClick(int row, int col)
    {
        if (!GameManager.Instance.Client.IsMyTurn) return;
        _ = GameManager.Instance.Client.SendGameMove(row, col);
    }

    public void MakeMove(int row, int col)
    {
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

        cells[row, col].SetCell(currentPlayerSymbol == "X" ? xSprite : oSprite);
    }
}
