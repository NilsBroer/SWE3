using System.Collections.Generic;
using SWE3.BusinessLogic.Interfaces;

namespace SWE3.BusinessLogic
{
    public class TableObject : ITableObject
    {
        public string tableName { get; set; }
        public List<(string name, string type)> columns { get; set; }
        public string primaryKey { get; set; }
    }
}