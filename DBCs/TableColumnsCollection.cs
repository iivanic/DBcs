using System.Data;
using System.Reflection;

namespace DBcs;

public class TableColumnsCollection<T> : List<TableColumns>
{
    public event EventHandler<TableChangedEventArgs> TableChanged;
    public List<T> ReturnCollection { get; set; } = new List<T>();

    public TableColumns AddTable(string tableName, int columnIndex,
        IDataReader dr, Type type)
    {
        if (tableName == null)
            throw new ArgumentNullException("tableName");
        TableColumns? t = null;
        foreach (var t1 in this)
            if (t1.Name == tableName)
                t = t1;

        if (t == null)
        {
            t = new TableColumns
            {
                Name = tableName
                
            };
            this.Add(t);
            t.TableIndex = Count - 1;
        }
        t.ColumnIndices.Add(columnIndex);
        t.TmpValues +=
            dr.IsDBNull(columnIndex) ? "NULL;" : dr.GetValue(columnIndex).ToString() + ";";
        return t;
    }
  
    public void Reset()
    {
        Clear();
    }
      
}
public class TableChangedEventArgs : EventArgs
{
    public Type RootType { get; set; }
    public IDataReader DataReader { get; set; }
    public TableColumns Table { get; set; }
    
}
