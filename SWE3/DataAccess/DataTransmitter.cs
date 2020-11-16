using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using Serilog;
using SWE3.DataAccess.Interfaces;

//TODO: Refactor and add more documentation (See CreateSqlTableFromShell) #2

namespace SWE3.DataAccess
{
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
        /// Builds a table-object and from that a new SQL-table according to the properties of the given object(-shell).
        /// Object can be empty, as only the shell (properties) is required.
        /// </summary>
        /// <param name="shell"></param>
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
                "I_AI_ID decimal IDENTITY(1,1), "; //Internal Auto-Increment ID for mapping, like second hidden primary key

            //TODO: Merge multiple primary keys to one constraint #1
            //(No foreign keys needed, you can still query correctly using your own logic, it's just not enforced)
            foreach (var column in table.Columns)
            {
                var customOrEnumerable = column.Type.Contains(CUSTOM) || column.Type.Contains(MULTIPLE);
                if (!customOrEnumerable)
                {
                    commandText +=
                        $"{column.Name} {column.Type}" +
                        (column.NotNull ? " NOT NULL" : "") +
                        (column.Unique ? " UNIQUE" : "") +
                        (column.PrimaryKey ? " PRIMARY KEY" : "") + ", ";
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

            commandText = commandText.Substring(0, commandText.Length - 2) + ");";
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

        public void InsertIntoSqlTable(object instance)
        {
            var table = instance.ToTable();
            
            if (InsertionQueue.Contains((table.Name,Iteration))) return;
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
                        InsertIntoSqlTable(values[i]);
                        var objectId = GetNextAutoIncrementForSqlTable(column.Name);
                        InsertIntoSqlHelperTable(table.Name, column.Name, internalId, objectId);
                    }
                    else
                    {
                        logger.Information("Inserting multiple custom-type-based values.");
                        column.Type = column.Type.Replace(MULTIPLE, "");
                        column.Type = column.Type.Replace(CUSTOM, "");
                        foreach (var value in (values[i] as IEnumerable)!)
                        {
                            InsertIntoSqlTable(value);
                            var objectId = GetNextAutoIncrementForSqlTable(value.GetType().Name);
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
                throw;
            }
        }

        //Helper-tables are used for 1:m and n:m relations alike, as it simplifies the lookup-logic //TODO: Re-evaluate? #0
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
            if (currentAutoIncrement == DBNull.Value) currentAutoIncrement = 1;
            return Convert.ToInt32(currentAutoIncrement);
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

//NOTE: Can not always map nested enumerables correctly
//(e.g. lists of different types in a list, but that would most often use an object anyways, so it's ok)