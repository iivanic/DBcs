using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBcs
{
    internal class DatabaseTableRow
    {
        public bool IsKey { get; set; }
        public ForeignKey? ForeignKey { get; set; }
        public bool IsDBNull { get; set; }
        public string Name { get; set; } = "";
        public string PropertyName { get; set; } = "";
        public string PropertyType { get; set; } = "";
        public string DotNetPropertyType { get; set; } = "";
        public string Comment { get; set; } = "";
        public bool IsInDb { get; set; } = true;
        public string GenerateClassPropertyText()
        {
            string ret = "";

            if (IsKey)
                ret += $"\t[Key]{Environment.NewLine}";

            if (ForeignKey != null)
            {
                if (ForeignKey.ReferencedTable != null)
                {
                    ret += $"\t[ForeignKey(\"{ForeignKey.ReferencedTable.ClassName}\")]{Environment.NewLine}";
                    Comment += $"// property for refernced object";
                }
            }
            if (!string.IsNullOrEmpty(Comment))
            {
                ret += $"\t{Comment}{Environment.NewLine}";
            }
            ret += $"\tpublic {PropertyType}{(IsDBNull ? "?" : "")} {PropertyName} {{get; set;}}{getDefault()}{Environment.NewLine}";
            if (ForeignKey != null)
            {
                if (ForeignKey.ReferencedTable != null)
                {
                    ret += $"\t[NotMapped]{Environment.NewLine}\tpublic {ForeignKey.ReferencedTable.ClassName}{(IsDBNull ? "?" : "")} {ForeignKey.ReferencedTable.ClassName} {{get; set;}}{getDefault()}{Environment.NewLine}";
                }
            }

            return ret;
        }
        private string getDefault()
        {
            string ret = "";
            if (IsDBNull == false)
            {
                if (!IsValueType(DotNetPropertyType))
                {

                    if (PropertyType == "string")
                        return " = string.Empty;";

                    if (PropertyType == "string[]")
                        return " = [];";
                    ret = $" = new {PropertyType}();";
                }
            }
            return ret;

        }


        private bool IsValueType(string type)
        {
            Type t = Type.GetType(type);
            if (t == null)
                return false;
            return t.IsValueType;


        }
    }
}
