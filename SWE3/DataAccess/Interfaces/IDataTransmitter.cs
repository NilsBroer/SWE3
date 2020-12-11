using System;
using SWE3.BusinessLogic;
using SWE3.BusinessLogic.Entities;

namespace SWE3.DataAccess.Interfaces
{
    public interface IDataTransmitter
    {
        public void CreateSqlTableFromShell(object shell);
        public int InsertIntoSqlTable(object instance);
        public void DeleteByIdWithReferences(int id,  Type type = null, object instance = null);

        public void DeleteByIdWithoutReferences(int id, Type type = null, string tableName = null, object instance = null);
        public int UpdateByIdWithReferences(int id, object instance);
        public void UpdateByIdWithoutReferences(int id, object instance);
        public void UpdateWithSingleParameter(int id, string tableName, string parameterName, dynamic parameterValue);
    }
}