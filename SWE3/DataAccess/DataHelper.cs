using System.Data;
using System.Data.SqlClient;
using SWE3.DataAccess.Interfaces;

namespace SWE3.DataAccess
{
    public class DataHelper : IDataHelper
    {
        private const string connectionString = @"Server=(LocalDb)\MSSQLLocalDB;Initial Catalog=SWE3;Integrated Security=SSPI;Trusted_Connection=yes;";
        private readonly SqlConnection connection;

        public DataHelper()
        {
            connection = GetConnection();;
        }

        private SqlConnection GetConnection()
        {

            SqlConnection con = new SqlConnection(connectionString);
            if (con.State != ConnectionState.Open)
                con.Open();

            return con;
        }

        public SqlCommand CreateCommand(string commandText)
        {
            return new SqlCommand(commandText, connection);
        }
        
        public string GetTestData()
        {
            var command = CreateCommand("SELECT TOP 1 * FROM dev_test");
            var reader = command.ExecuteReader();
            if (!reader.HasRows)
            {
                reader.Close();
                return "-1: ERROR";
            }

            reader.Read();

            var number = (int) reader[0];
            return (int) reader[0] + ": " + (string) reader[1];
        }
    }
}