using System;
using System.Data.SqlClient;
using System.Linq;
using SWE3.BusinessLogic.Entities;
using SWE3.DataAccess.Interfaces;

namespace SWE3.DataAccess
{
    public class SqlMapper : ISqlMapper
    {
        private readonly IDataHelper dataHelper;

        public SqlMapper(IDataHelper dataHelper)
        {
            this.dataHelper = dataHelper;
        }
        /// <summary>
        /// Builds a new SQL-table according to the properties of the given object.
        /// Object can be empty, as only the shell (properties) is required.
        /// </summary>
        /// <param name="table"></param>
        public void CreateSqlTable(Table table)
        {
            var commandText = $"CREATE TABLE {table.Name} (";

            //TODO: IMPORTANT! Multiple primary key constraints
            foreach (var column in table.Columns) //TODO: What about ForeignKey and Constraints?
            {
                commandText += 
                    $"{column.Name} {column.Type}" +
                    (column.NotNull || column.PrimaryKey || column.SecondaryKey ? " NOT NULL" : "") +
                    (column.PrimaryKey ? " PRIMARY KEY" : "") + ", ";
            }
            commandText = commandText.Substring(0, commandText.Length - 2) + ");";

            //Console.WriteLine(commandText);
            
            var command = dataHelper.CreateCommand(commandText);
            command.ExecuteNonQuery();
        }
        
        /// <summary>
        /// Fills an existing SQL-table with the values of the given object.
        /// </summary>
        /// <param name="instance"></param>
        public void InsertIntoSqlTable(object instance)
        {
            var properties = instance.GetType().GetProperties();
            var propertyValues = properties.Select(property => property.GetValue(instance)).ToList();

            var table = instance.ToTable();
            var columnNames = table.Columns.Select(column => column.Name).ToList();

            //Build the command with parameters
            var commandText = $"INSERT INTO {table.Name} (";

            commandText = columnNames.Aggregate(commandText,
                (text, columnName) => text + columnName + ", ");
            commandText = commandText.Substring(0, commandText.Length - 2) + ")" +
                          "\n" + "VALUES (";
            
            for (var i = 0; i < columnNames.Count; i++)
            {
                commandText += $"@param{i}, ";
            }
            commandText = commandText.Substring(0, commandText.Length - 2) + ");";
            
            Console.WriteLine(commandText);

            var command = dataHelper.CreateCommand(commandText);
            
            //Fill the command with the respective parameters and execute
            foreach ((var value, int i) in propertyValues.Select((value, i) => (value, i)))
            {
                command.Parameters.Add( new SqlParameter($"@param{i}", value) );
            }

            command.ExecuteNonQuery();
        }

        //TODO: how to handle functions
        //TODO: primary key
    }
}