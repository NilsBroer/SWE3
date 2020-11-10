using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using Serilog.Core;
using SWE3.BusinessLogic.Entities;
using SWE3.DataAccess.Interfaces;

namespace SWE3.DataAccess
{
    public class SqlMapper : ISqlMapper
    {
        private readonly IDataHelper dataHelper;

        private const string ENUMERABLE = "enumerable_";
        private const string CUSTOM = "custom_";

        private static List<string> Queue = new List<string>();

        public SqlMapper(IDataHelper dataHelper)
        {
            this.dataHelper = dataHelper;
        }

        /// <summary>
        /// Builds a table-object and from that a new SQL-table according to the properties of the given object(-shell).
        /// Object can be empty, as only the shell (properties) is required.
        /// </summary>
        /// <param name="shell"></param>
        public void CreateSqlTableFromShell(object shell)
        {
            var table = shell.ToTable();
            if (Queue.Contains(table.Name) || TableExists(table.Name)) return;
            Queue.Add(table.Name);

            var commandText =
                $"CREATE TABLE {table.Name} (" +
                "I_AI_ID int IDENTITY(1,1), "; //Internal Auto-Increment ID for mapping, not official primary key

            //TODO: Merge for multiple primary keys
            foreach (var column in table.Columns) //TODO: What about ForeignKey (and Constraints)?
            {
                var customOrEnumerable = column.Type.Contains(CUSTOM) || column.Type.Contains(ENUMERABLE);
                commandText +=
                        $"{(!customOrEnumerable ? column.Name : column.Name + "_ID")} {(!customOrEnumerable ? column.Type : "int")}" +
                        (column.NotNull ? " NOT NULL" : "") +
                        (column.Unique ? " UNIQUE" : "") +
                        (column.PrimaryKey ? " PRIMARY KEY" : "") + ", ";
                    //Note that for these, the values below imply the values above in SQL

                    if (customOrEnumerable)
                    {
                        if (!column.Type.Contains(CUSTOM))
                        {
                            column.Type = column.Type.Replace(ENUMERABLE, "");
                            CreateSqlHelperTable(table.Name, column.Name, column.Type, false);
                        }
                        else
                        {
                            column.Type = column.Type.Replace(ENUMERABLE, ""); //Current logic doesn't care
                            column.Type = column.Type.Replace(CUSTOM, "");
                            CreateSqlHelperTable(table.Name, column.Name);
                            
                            var type = GetType(column.Type);
                            var subShell = type != null ? Assembly.GetAssembly(type)?.CreateInstance(column.Type) : null;
                            if (subShell != null)
                            {
                                CreateSqlTableFromShell(subShell);
                            }
                            else
                            {
                                Console.WriteLine(type);
                                throw new Exception("Could not find Assembly for shell of sub-type");
                            }
                        }
                    }
            }
            commandText = commandText.Substring(0, commandText.Length - 2) + ");";
            Console.WriteLine(commandText);
            var command = dataHelper.CreateCommand(commandText);
            command.ExecuteNonQuery();
        }

        public void CreateSqlHelperTable(string supTableName, string name, string sqlType = "int", bool isActualHelperTable = true)
        {
            if (TableExists(supTableName + "_" + name))
            {
                //TODO: Log table already exists
                return;
            }
            var commandText =
                $"CREATE TABLE {supTableName}_{name} (" +
                "I_AI_ID int IDENTITY(1,1)," +
                $"{(!isActualHelperTable ? name : name + "_ID")} {sqlType} NOT NULL" +
                ");";
            var command = dataHelper.CreateCommand(commandText);
            command.ExecuteNonQuery();
        }

        public void InsertIntoSqlTable(object instance)
            //TODO: Update this to also insert into sub-tables using internal IDs #3
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

        private Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null) return type;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }

        private bool TableExists(string tableName)
        {
            var commandText = 
                "SELECT CASE WHEN EXISTS" +
                $"(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}') " +
                " THEN 1 ELSE 0 END";
            var command = dataHelper.CreateCommand(commandText);

            return (int) command.ExecuteScalar() == 1;
        }
    }
}