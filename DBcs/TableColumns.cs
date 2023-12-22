namespace DBcs;

public class TableColumns
{
    public string Name { get; set; } = "";
    public string PropName { get; set; } = "";
    public object Object { get; set; } = "";
    public string Values { get; set; } = "";
    public string TmpValues { get; set; } = "";
    public List<int> ColumnIndices { get; set; } = new();
    public int TableIndex { get; set; } = -1;
}