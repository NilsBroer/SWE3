using SWE3.BusinessLogic;

namespace SWE3.DataAccess.Interfaces
{
    public interface ISqlMapper
    {
        public void CreateSqlTable(TableObject tableObject);
        public void InsertIntoSqlTable(object instance);
    }
}