namespace SWE3.BusinessLogic.Entities.Interfaces
{
    public interface IColumn
    {
        public string Name { get; set; }
        public string Type { get; set; }
        
        public bool NotNull { get; set; }
        public bool PrimaryKey { get; set; }
        public bool Unique { get; set; }
    }
}