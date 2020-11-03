using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using SWE3.BusinessLogic.Entities;
using SWE3.DataAccess.Interfaces;

namespace SWE3.DataAccess
{
    public class SqlMapper : ISqlMapper
    {
        private readonly IDataHelper dataHelper;

        private const string TABLE = "table_";
        private const string SYSTEM = "System.";

        public SqlMapper(IDataHelper dataHelper)
        {
            this.dataHelper = dataHelper;
        }

        /// <summary>
        /// Builds a table-object and from that a new SQL-table according to the properties of the given object(-shell).
        /// Object can be empty, as only the shell (properties) is required.
        /// </summary>
        /// <param name="shell"></param>
        public void CreateSqlTable(object shell)
        {
            var table = shell.ToTable();
            var commandText = 
                $"CREATE TABLE {table.Name} (" + 
                "I_AI_ID int IDENTITY(1,1), "; //Internal Auto-Increment ID for mapping, not official primary key

            //TODO: IMPORTANT! Multiple primary key constraints
            foreach (var column in table.Columns) //TODO: What about ForeignKey and Constraints?
            {
                if (!column.Type.StartsWith(TABLE))
                {
                    commandText +=
                        $"{column.Name} {column.Type}" +
                        (column.NotNull ? " NOT NULL" : "") +
                        (column.Unique ? " UNIQUE" : "") +
                        (column.PrimaryKey ? " PRIMARY KEY" : "") + ", ";
                    //Note that for these, the values below imply the values above in SQL
                }
                else
                {
                    var typeString = column.Type.Remove(0,TABLE.Length);
                    if (typeString.StartsWith(SYSTEM)) //It's a known C#-type or an array, collection, etc of such
                    {
                        var subTable = new Table
                        {
                            Columns = new List<Column>
                            {
                                new Column
                                {
                                    Name = column.Name,
                                    Type = "" //Adjust after other TODO
                                }
                            },
                            Name = table.Name + "_" + column.Name
                        };
                    }
                    else //It's a custom type
                    {
                        var type = Type.GetType(typeString);
                        var subShell = type != null ? Assembly.GetAssembly(type)?.CreateInstance(typeString) : null;
                        if (subShell != null)
                        {
                            CreateSqlTable(subShell.ToTable());
                        }
                    }
                    //TODO: Do this and also only create if doesnt exist
                    //If you can get a class reference by that:
                    //Otherwise also create a table and fill it with the single values found
                }
            }
            commandText = commandText.Substring(0, commandText.Length - 2) + ");";

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
    }
}