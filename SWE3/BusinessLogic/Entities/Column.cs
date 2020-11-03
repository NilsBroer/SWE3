using SWE3.BusinessLogic.Entities.Interfaces;

namespace SWE3.BusinessLogic.Entities
{
    public class Column : IColumn
    {
        public string Name { get; set; }
        public string Type { get; set; }
        
        public bool NotNull { get; set; }
        public bool PrimaryKey { get; set; }
        public bool Unique { get; set; }
    }
}