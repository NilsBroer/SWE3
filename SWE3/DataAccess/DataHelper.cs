using System.Data;
using System.Data.SqlClient;
using SWE3.DataAccess.Interfaces;

namespace SWE3.DataAccess
{
    /// <inheritdoc />
    public class DataHelper : IDataHelper
    {
        private const string connectionString = @"Server=(LocalDb)\MSSQLLocalDB;Initial Catalog=SWE3;Integrated Security=SSPI;Trusted_Connection=yes;MultipleActiveResultSets=True;";
        private const string testConnectionString = @"Server=(LocalDb)\MSSQLLocalDB;Initial Catalog=SWE3Test;Integrated Security=SSPI;Trusted_Connection=yes;MultipleActiveResultSets=True;";
        private readonly SqlConnection connection;

        public DataHelper(bool useRealDatabase = true)
        {
            connection = useRealDatabase ? GetConnection() : GetTestConnection();
        }

        /// <summary>
        /// Connects with the connection string set above (or in the appSettings, if not)
        /// </summary>
        private SqlConnection GetConnection()
        {

            SqlConnection con = new SqlConnection(connectionString);
            if (con.State != ConnectionState.Open)
            {
                con.Open();
            }

            return con;
        }

        /// <summary>
        /// Connects with the connection string for testing set above (or in the appSettings, if not)
        /// </summary>
        private SqlConnection GetTestConnection()
        {
            SqlConnection con = new SqlConnection(testConnectionString);
            if (con.State != ConnectionState.Open)
            {
                con.Open();
            }

            return con;
        }

        /// <inheritdoc />
        public SqlCommand CreateCommand(string commandText)
        {
            return new SqlCommand(commandText, connection);
        }
        
        /// <inheritdoc />
        public void ClearDatabase()
        {
            this.CreateCommand("EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'").ExecuteNonQuery();
            this.CreateCommand("EXEC sp_MSForEachTable 'DELETE FROM ?'").ExecuteNonQuery();
            this.CreateCommand("EXEC sp_MSForEachTable 'ALTER TABLE ? CHECK CONSTRAINT ALL'").ExecuteNonQuery();
        }
    }
}