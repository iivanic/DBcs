using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBcs
{
    internal class DatabaseTable
    {
        public string TableName { get; set; } = "";
        public string FullTableName { get; set; } = "";
        public string ClassName { get; set; } = "";
        public List<DatabaseTableRow> Rows { get; set; } = [];
        public List<DatabaseTableRow> PrimaryKeys { get; set; } = [];
        public List<DatabaseTableRow> ForeignKeys { get; set; } = [];
        public string CreateSelectText()
        {
            return $"select * from {TableName};";
        }
        public string CreateUpdateText()
        {
            string cols = "";
            int c = 0;
            foreach (var row in Rows)
            {
                if (!row.IsKey && row.IsInDb)
                {
                    cols += $"{(c > 0 ? ", " : "")}{row.Name}=@{row.Name}";
                    c++;
                }
            }
            string ret = $"update {TableName} set {cols} where {CreateWhere()} returning *;";

            return ret;
        }
        public string CreateInsertText()
        {
            string cols = "";
            string colsP = "";
            int c = 0;
            foreach (var row in Rows)
            {
                if (!row.IsKey && row.IsInDb)
                {
                    cols += $"{(c > 0 ? ", " : "")}{row.Name}";
                    colsP += $"{(c > 0 ? ", " : "")}@{row.Name}";
                    c++;
                }
            }

            return $"insert into {TableName} ({cols}) values({colsP})  returning *;";
        }
        public string CreateDeleteText()
        {
            return $"delete from {TableName} where {CreateWhere()};";
        }
        private string CreateWhere()
        {
            string where = "";
            int w = 0;
            foreach (var row in PrimaryKeys)
            {
                where += $"{(w > 0 ? " and " : "")}{row.Name}=@{row.Name}";
                w++;
            }
            return where;
        }

        public override string ToString()
        {
            return TableName;
        }

        public string GenerateClassText()
        {
            string ret = "";
            ret += $"{Environment.NewLine}[Table(\"{TableName}\")]{Environment.NewLine}";
            ret += $"public class {ClassName}{Environment.NewLine}{{{Environment.NewLine}";


            foreach (var row in Rows)
            {
                ret += row.GenerateClassPropertyText();
            }

            ret += $"\t// Not used by DBHelp directly{Environment.NewLine}";
            ret += $"\tpublic const string SelectText = \"{CreateSelectText()}\";{Environment.NewLine}";
            ret += $"\tpublic const string UpdateText = \"{CreateUpdateText()}\";{Environment.NewLine}";
            ret += $"\tpublic const string InsertText = \"{CreateInsertText()}\";{Environment.NewLine}";
            ret += $"\tpublic const string DeleteText = \"{CreateDeleteText()}\";{Environment.NewLine}";

            ret += $"{Environment.NewLine}}}{Environment.NewLine}";

            return ret;
        }


    }
}
