using System.Data.SqlClient;

namespace SWE3.DataAccess.Interfaces
{
    public interface IDataHelper
    {
        public SqlCommand CreateCommand(string commandText);
        public string GetTestData();

    }
}