using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBcs
{
    internal class ForeignKey
    {
        public string TableName { get; set; } = "";
        public string TableFullName { get { return $"{TableSchema}.{TableName}";  } }
        public string TableSchema { get; set; } = "";
        public string ColumnName { get; set; } = "";
        public string ReferencedTableName { get; set; } = "";
        public string ReferencedFulllTableName  { get { return $"{ReferencedTableSchema}.{ReferencedTableName}";  }}
        public string ReferencedTableSchema { get; set; } = "";
        public DatabaseTable? ReferencedTable { get; set; } 

    }
}
