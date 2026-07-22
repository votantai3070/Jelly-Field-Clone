using UnityEngine;

[System.Serializable]
public class CellData
{
    public Vector2Int Coord;
    public JellyPiece OccupiedPiece;

    public JellyPiece CurrentPiece => OccupiedPiece;
    public bool IsEmpty => OccupiedPiece == null;

    public CellData(Vector2Int coord)
    {
        Coord = coord;
    }

    public void SetPiece(JellyPiece piece)
    {
        OccupiedPiece = piece;
    }

    public void Clear()
    {
        OccupiedPiece = null;
    }
}