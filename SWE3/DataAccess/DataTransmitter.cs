using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using Serilog;
using SWE3.DataAccess.Interfaces;

//TODO: Add UPDATE (use update on same primary key? --> bool) see next todo
//TODO: Evaluate logic when insert and when update gets triggered (primary key constraint)
//TODO: Give ability to add foreign key constraints afterwards (alter table) and query by that (maybe)
//TODO: Refactor #last

namespace SWE3.DataAccess
{
    /// <summary>
    /// Transmitts data from C#-code to SQL(-tables).
    /// Class --> table (Create)
    /// Instance --> Entry (Insert)
    /// </summary>
    public class DataTransmitter : IDataTransmitter
    {
        private readonly IDataHelper dataHelper;
        private readonly ILogger logger;

        private const string MULTIPLE = "#multiple#";
        private const string CUSTOM = "#custom#";

        private static readonly List<string> CreationQueue = new List<string>();
        private static readonly List<(string insert, int iteration)> InsertionQueue = new List<(string,int)>();
        private static int Iteration;

        public DataTransmitter(IDataHelper dataHelper, ILogger logger)
        {
            this.dataHelper = dataHelper;
            this.logger = logger;
        }

        /// <summary>
        /// Builds a table-object and from that a new SQL-table according to the properties of the given class.
        /// Object can be empty, as only the shell (class-properties) is required.
        /// </summary>
        public void CreateSqlTableFromShell(object shell)
        {
            var table = shell.ToTable();
            if (CreationQueue.Contains(table.Name) || TableExists(table.Name))
            {
                logger.Information("Table already created.");
                return;
            }
            CreationQueue.Add(table.Name);
            
            logger.Information("Starting creation of table.");
            
            var commandText =
                $"CREATE TABLE {table.Name} (" +
                "I_AI_ID decimal IDENTITY(1,1), "; //Internal Auto-Increment ID for mapping, like second, more importantly single, hidden primary key
            
            foreach (var column in table.Columns)
            {
                var customOrEnumerable = column.Type.Contains(CUSTOM) || column.Type.Contains(MULTIPLE);
                if (!customOrEnumerable)
                {
                    commandText +=
                        $"{column.Name} {column.Type}" +
                        (column.NotNull ? " NOT NULL" : "") +
                        (column.Unique || column.PrimaryKey ? " UNIQUE" : "") + ", ";
                }
                else
                {
                    if (!column.Type.Contains(CUSTOM))
                    {
                        column.Type = column.Type.Replace(MULTIPLE, "");
                        CreateSqlHelperTable(table.Name, column.Name, column.Type, false);
                    }
                    else
                    {
                        column.Type = column.Type.Replace(MULTIPLE, "");
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
                            logger.Error("Could not find Assembly for shell of sub-type", type);
                        }
                    }
                }
            }

            if (table.Columns.Any(column => column.PrimaryKey))
            {
                commandText += $"CONSTRAINT PK_{table.Name} PRIMARY KEY(";
                commandText = table.Columns.Where(column => column.PrimaryKey).Aggregate(commandText,
                    (current, primaryKeyColumn) => current + primaryKeyColumn.Name + ", ");
                commandText = commandText.Substring(0, commandText.Length - 2) + "));";

            }
            else
            {
                commandText = commandText.Substring(0, commandText.Length - 2) + ");";
            }
            logger.Debug(commandText);
            var command = dataHelper.CreateCommand(commandText);
            
            try
            {
                command.ExecuteNonQuery();
            }
            catch (SqlException e)
            {
                logger.Fatal("SqlException: ", e);
                throw;
            }
        }

        /// <summary>
        /// Creates a helper-table.
        /// Can represent a relation between two objects via their IDs (ObjectID (1) | ObjectID (2))
        /// or a relation between an object and a value directly (ObjectID | Value)
        /// </summary>
        private void CreateSqlHelperTable(string supTableName, string name, string sqlType = "int", bool isForCustomType = true)
        {
            if (TableExists(supTableName + "_x_" + name))
            {
                logger.Information("Table already created.");
                return;
            }

            logger.Information("Starting creation of helper-table.");
            var commandText =
                $"CREATE TABLE {supTableName}_x_{name} (" +
                $"{supTableName}_ID int NOT NULL, " +
                $"{(!isForCustomType ? name : name + "_ID")} {sqlType} NOT NULL" +
                ");";
            Console.WriteLine(commandText);
            var command = dataHelper.CreateCommand(commandText);
            
            try
            {
                command.ExecuteNonQuery();
            }
            catch (SqlException e)
            {
                logger.Fatal("SqlException: ", e);
                throw;
            }
        }

        /// <summary>
        /// Inserts the values held by an object (instance) into an already existing sql-table
        /// </summary>
        /// <returns>ID upon success (>= 1), 0 when redundant, -1 upon failure</returns>
        public int InsertIntoSqlTable(object instance)
        {
            var table = instance.ToTable();
            
            if (InsertionQueue.Contains((table.Name,Iteration))) return 0;
            InsertionQueue.Add((table.Name,Iteration));
            
            logger.Information("Started inserting into table.");

            var values = instance.GetType().GetProperties().Select(property => property.GetValue(instance)).ToArray();
            var internalId = GetNextAutoIncrementForSqlTable(table.Name);

            //Build the command with parameters
            var commandTextInsertPart = $"INSERT INTO {table.Name} (";
            var commandTextValuesPart = "\n" + "VALUES (";
            var parameterIndices = new List<int>();

            foreach ((var column, int i) in table.Columns.Select((column, i) => (column, i)))
            {
                var customOrEnumerable = column.Type.Contains(CUSTOM) || column.Type.Contains(MULTIPLE);
                if (customOrEnumerable)
                {
                    if (values[i] == null)
                    {
                        logger.Error("Expected object or enumerable to map, found null instead.", values[i], i, values);
                        throw new Exception();
                    }
                    
                    if (!column.Type.Contains(CUSTOM))
                    {
                        logger.Information("Inserting multiple system-type-based values.");
                        column.Type = column.Type.Replace(MULTIPLE, "");
                        foreach (var value in (values[i] as IEnumerable)!)
                        {
                            InsertIntoSqlHelperTable(table.Name, column.Name, internalId, value, false);
                        }
                    }
                    else if(!column.Type.Contains(MULTIPLE))
                    {
                        logger.Information("Inserting a single custom-type-based value.");
                        column.Type = column.Type.Replace(CUSTOM, "");
                        var objectId = InsertIntoSqlTable(values[i]);
                        InsertIntoSqlHelperTable(table.Name, column.Name, internalId, objectId);
                    }
                    else
                    {
                        logger.Information("Inserting multiple custom-type-based values.");
                        column.Type = column.Type.Replace(MULTIPLE, "");
                        column.Type = column.Type.Replace(CUSTOM, "");
                        foreach (var value in (values[i] as IEnumerable)!)
                        {
                            var objectId = InsertIntoSqlTable(value);
                            Iteration++;
                            InsertIntoSqlHelperTable(table.Name, column.Name, internalId, objectId);
                        }
                    }
                }
                else
                {
                    commandTextInsertPart += column.Name + ", ";
                    commandTextValuesPart += $"@param{i}, ";
                    parameterIndices.Add(i);
                }
            }
            commandTextInsertPart = commandTextInsertPart.Substring(0, commandTextInsertPart.Length - 2) + ")";
            commandTextValuesPart = commandTextValuesPart.Substring(0, commandTextValuesPart.Length - 2) + ");";
            var commandText = commandTextValuesPart.Contains("@param")
                ? commandTextInsertPart + commandTextValuesPart
                : $"INSERT INTO {table.Name} DEFAULT VALUES;";
            
            var command = dataHelper.CreateCommand(commandText);

            logger.Information("Filling command with parameterized values.");
            foreach ((var value, int i) in values.Where((value, index) => 
                parameterIndices.Contains(index)).Select((value, i) => (value, i)))
            {
                command.Parameters.Add(new SqlParameter($"@param{i}", value ?? DBNull.Value));
            }

            try
            {
                command.ExecuteNonQuery();
            }
            catch (SqlException e)
            {
                logger.Fatal("SqlException: ", e);
                return -1;
            }

            return internalId;
        }
        
        /// <summary>
        /// Inserts the values held in any instance of an enumerable
        /// or connects an object with another object
        /// or connects an object with a value
        /// </summary>
        private void InsertIntoSqlHelperTable(string supTableName, string name, int internalId, dynamic value, bool isForCustomType = true)
        {
            logger.Information("Started inserting into helper-table.");
            var commandText =
                $"INSERT INTO {supTableName}_x_{name} (" +
                $"{supTableName}_ID, {(!isForCustomType ? name : name + "_ID")})" +
                $"VALUES ({internalId}, {value});";
            var command = dataHelper.CreateCommand(commandText);
            
            try
            {
                command.ExecuteNonQuery();
            }
            catch (SqlException e)
            {
                logger.Fatal("SqlException: ", e);
                throw;
            }
        }

        /// <summary>
        /// Looks up an sql-table by name and returns the next iteration of its auto-increment-identity.
        /// </summary>
        private int GetNextAutoIncrementForSqlTable(string tableName)
        {
            logger.Information("Getting next identity-value.");
            var command = dataHelper.CreateCommand($"SELECT IDENT_CURRENT('{tableName}');");
            object currentAutoIncrement;
            try
            {
                currentAutoIncrement = command.ExecuteScalar();
            }
            catch (SqlException e)
            {
                logger.Fatal("SqlException: ", e);
                throw;
            }
            if (currentAutoIncrement == DBNull.Value) currentAutoIncrement = 0;
            return Convert.ToInt32(currentAutoIncrement) + 1;
        }

        /// <summary>
        /// Gets and returns an actual type by its name in string-form.
        /// </summary>
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

        /// <summary>
        /// Checks (and returns true) if a table exists.
        /// </summary>
        private bool TableExists(string tableName)
        {
            logger.Information("Checking if table already exists.");
            var commandText =
                "SELECT CASE WHEN EXISTS" +
                $"(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}') " +
                " THEN 1 ELSE 0 END";
            var command = dataHelper.CreateCommand(commandText);

            return (int) command.ExecuteScalar() == 1;
        }
    }
}