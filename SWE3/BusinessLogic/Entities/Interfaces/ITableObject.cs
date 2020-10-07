using System;
using System.Collections.Generic;

namespace SWE3.BusinessLogic.Interfaces
{
    public interface ITableObject
    {
        public string tableName { get; set; }
        public List<(string name, string type)> columns { get; set; }
        public string primaryKey { get; set; }
    }
}