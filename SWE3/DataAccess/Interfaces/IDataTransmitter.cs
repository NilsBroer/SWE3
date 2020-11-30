using SWE3.BusinessLogic;
using SWE3.BusinessLogic.Entities;

namespace SWE3.DataAccess.Interfaces
{
    public interface IDataTransmitter
    {
        public void CreateSqlTableFromShell(object shell);
        public int InsertIntoSqlTable(object instance);
    }
}