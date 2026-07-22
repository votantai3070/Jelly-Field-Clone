using System;
using UnityEngine;

[Serializable]
public class CellData
{
    [SerializeField] private Vector2Int coord;
    [SerializeField] private JellyPiece currentPiece;

    public Vector2Int Coord
    {
        get => coord;
        set => coord = value;
    }

    public JellyPiece CurrentPiece
    {
        get => currentPiece;
        private set => currentPiece = value;
    }

    public bool IsEmpty => currentPiece == null;

    public CellData(Vector2Int coord)
    {
        this.coord = coord;
        currentPiece = null;
    }

    public void SetPiece(JellyPiece piece)
    {
        CurrentPiece = piece;
    }

    public void Clear()
    {
        CurrentPiece = null;
    }

    public bool HasPiece(JellyPiece piece)
    {
        return currentPiece == piece;
    }
}