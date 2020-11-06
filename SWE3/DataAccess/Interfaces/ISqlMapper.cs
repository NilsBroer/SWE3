using SWE3.BusinessLogic;
using SWE3.BusinessLogic.Entities;

namespace SWE3.DataAccess.Interfaces
{
    public interface ISqlMapper
    {
        public void CreateSqlTableFromShell(object shell);
        public void InsertIntoSqlTable(object instance);
    }
}