using System;
using System.Collections.Generic;
using SWE3.BusinessLogic.Entities;

namespace SWE3.BusinessLogic.Entities.Interfaces
{
    public interface ITable
    {
        public string Name { get; set; }
        public List<Column> Columns { get; set; }
    }
}