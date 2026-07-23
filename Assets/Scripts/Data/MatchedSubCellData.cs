[System.Serializable]
public class MatchedSubCellData
{
    public JellyPiece piece;
    public string subCellId;
    public JellyColor color;
    public JellySlot slot;

    public MatchedSubCellData(JellyPiece piece, string subCellId, JellyColor color, JellySlot slot)
    {
        this.piece = piece;
        this.subCellId = subCellId;
        this.color = color;
        this.slot = slot;
    }
}
