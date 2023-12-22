using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBcsDemo
{
    internal class SQLDBcs:DBcs.DBcs
    {
        public override DbDataSource CreateDataSource()
        {
            return SqlClientFactory.Instance.CreateDataSource("<connection string>"); 
        }
    }
}
