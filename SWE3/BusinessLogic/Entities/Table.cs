using System.Collections.Generic;
using SWE3.BusinessLogic.Entities.Interfaces;

namespace SWE3.BusinessLogic.Entities
{
    public class Table : ITable
    {
        public string Name { get; set; }
        public List<Column> Columns { get; set; }
    }
}