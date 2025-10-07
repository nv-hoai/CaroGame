using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Board : MonoBehaviour, IDropHandler
{
    public int boardSize = 15;
    public GameObject cellPrefab;
    public Sprite blankSprite;
    public Sprite xSprite;
    public Sprite oSprite;
    public Avatar p1, p2;

    private Cell[,] cells;
    private GridLayoutGroup grid;

    void Start()
    {
        grid = gameObject.GetComponent<GridLayoutGroup>();
        cells = new Cell[boardSize, boardSize];
        CreateBoard();
    }

    public void CreateBoard()
    {
        grid.constraintCount = boardSize;
        for (int x = 0; x < boardSize; x++)
        {
            for (int y = 0; y < boardSize; y++)
            {
                int ix, iy;
                ix = x; iy = y;

                GameObject cellObject = Instantiate(cellPrefab, gameObject.transform);
                Cell cell = cellObject.GetComponent<Cell>();
                Button btn = cell.GetComponent<Button>();
                
                cells[x, y] = cell;
                btn.onClick.AddListener(() => OnCellClick(ix, iy));
            }
        }
    }

    public void SetAvatar()
    {
        p1.SetPlayerName(GameManager.Instance.Client.MyPlayerInfo.playerName);
        p2.SetPlayerName(GameManager.Instance.Client.OpponentInfo.playerName);
    }

    public void ResetBoard()
    {
        foreach (Cell cell in cells)
        {
            cell.SetCell(blankSprite);
        }
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

    public void OnDrop(PointerEventData eventData)
    {
        throw new System.NotImplementedException();
    }
}
