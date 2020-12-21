using System.Data.SqlClient;

namespace SWE3.DataAccess.Interfaces
{
    /// <summary>
    /// Helps with sql-based interactions in general.
    /// </summary>
    public interface IDataHelper
    {
        /// <summary>
        /// Creates a SqlCommand on the basis of a given text and with the pre-established connection
        /// </summary>
        public SqlCommand CreateCommand(string commandText);

        /// <summary>
        /// Deletes all entries from the pre-set MSSQL database
        /// </summary>
        public void ClearDatabase();

        //TODO: Transactions or similiar?
    }
}