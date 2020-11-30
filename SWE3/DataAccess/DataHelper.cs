using System.Data;
using System.Data.SqlClient;
using SWE3.DataAccess.Interfaces;

namespace SWE3.DataAccess
{
    public class DataHelper : IDataHelper
    {
        private const string connectionString = @"Server=(LocalDb)\MSSQLLocalDB;Initial Catalog=SWE3;Integrated Security=SSPI;Trusted_Connection=yes;MultipleActiveResultSets=True;";
        private const string testConnectionString = @"Server=(LocalDb)\MSSQLLocalDB;Initial Catalog=SWE3Test;Integrated Security=SSPI;Trusted_Connection=yes;MultipleActiveResultSets=True;";
        private readonly SqlConnection connection;

        public DataHelper(bool useRealDatabase = true)
        {
            connection = useRealDatabase ? GetConnection() : GetTestConnection();
        }

        private SqlConnection GetConnection()
        {

            SqlConnection con = new SqlConnection(connectionString);
            if (con.State != ConnectionState.Open)
            {
                con.Open();
            }

            return con;
        }

        private SqlConnection GetTestConnection()
        {
            SqlConnection con = new SqlConnection(testConnectionString);
            if (con.State != ConnectionState.Open)
            {
                con.Open();
            }

            return con;
        }

        public SqlCommand CreateCommand(string commandText)
        {
            return new SqlCommand(commandText, connection);
        }
    }
}