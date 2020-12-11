using System.Data.SqlClient;

namespace SWE3.DataAccess.Interfaces
{
    public interface IDataHelper
    {
        public SqlCommand CreateCommand(string commandText);
        
        //TODO: Cleare whole DB-function
        //TODO: Transactions or similiar?
    }
}