using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using SWE3.DataAccess.Interfaces;

namespace SWE3.DataAccess
{
    public class SqlMapper : ISqlMapper
    {
        private readonly IDataHelper dataHelper;

        private const string MULTIPLE = "#multiple#";
        private const string CUSTOM = "#custom#";

        private static readonly List<string> CreationQueue = new List<string>();
        private static readonly List<(string insert,int iteration)> InsertionQueue = new List<(string,int)>();
        public static int Iteration = 0;

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
            if (CreationQueue.Contains(table.Name) || TableExists(table.Name)) return;
            CreationQueue.Add(table.Name);

            var commandText =
                $"CREATE TABLE {table.Name} (" +
                "I_AI_ID decimal IDENTITY(1,1), "; //Internal Auto-Increment ID for mapping, like second hidden primary key

            //TODO: Merge for multiple primary keys
            foreach (var column in table.Columns) //TODO: What about ForeignKey (and Constraints)?
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
                            Console.WriteLine(type);
                            throw new Exception("Could not find Assembly for shell of sub-type");
                            //TODO: Make custom exception (unimportant) - Also custom-errors in general, think about throws
                        }
                    }
                }
            }

            commandText = commandText.Substring(0, commandText.Length - 2) + ");";
            Console.WriteLine(commandText);
            var command = dataHelper.CreateCommand(commandText);
            command.ExecuteNonQuery();
        }

        public void CreateSqlHelperTable(string supTableName, string name, string sqlType = "int", bool isForCustomType = true)
        {
            if (TableExists(supTableName + "_x_" + name))
            {
                //TODO: Add logging (Log table already exists) #4
                return;
            }

            var commandText =
                $"CREATE TABLE {supTableName}_x_{name} (" +
                $"{supTableName}_ID int NOT NULL, " +
                $"{(!isForCustomType ? name : name + "_ID")} {sqlType} NOT NULL" +
                ");";
            Console.WriteLine(commandText);
            var command = dataHelper.CreateCommand(commandText);
            command.ExecuteNonQuery();
        }

        public void InsertIntoSqlTable(object instance)
        {
            var table = instance.ToTable();
            
            if (InsertionQueue.Contains((table.Name,Iteration))) return;
            InsertionQueue.Add((table.Name,Iteration));

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
                    if (!column.Type.Contains(CUSTOM)) //Multiple, but not custom
                    {
                        column.Type = column.Type.Replace(MULTIPLE, "");
                        foreach (var value in values[i] as IEnumerable)
                        {
                            InsertIntoSqlHelperTable(table.Name, column.Name, internalId, value, false);
                        }
                    }
                    else if(!column.Type.Contains(MULTIPLE)) //Custom, but not multiple
                    {
                        column.Type = column.Type.Replace(CUSTOM, "");
                        InsertIntoSqlTable(values[i]);
                        var objectId = GetNextAutoIncrementForSqlTable(column.Name);
                        InsertIntoSqlHelperTable(table.Name, column.Name, internalId, objectId);
                    }
                    else //Both
                    {
                        column.Type = column.Type.Replace(MULTIPLE, "");
                        column.Type = column.Type.Replace(CUSTOM, "");
                        foreach (var value in values[i] as IEnumerable)
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

            //Fill the command with the respective parameters and execute
            foreach ((var value, int i) in values.Where((value, index) => 
                parameterIndices.Contains(index)).Select((value, i) => (value, i)))
            {
                command.Parameters.Add(new SqlParameter($"@param{i}", value ?? DBNull.Value));
            }

            command.ExecuteNonQuery();
        }

        //Helper-tables are used for 1:m and n:m relations alike, as it simplifies the lookup-logic //TODO: Re-evaluate?
        public void InsertIntoSqlHelperTable(string supTableName, string name, int internalId, dynamic value, bool isForCustomType = true)
        {
            var commandText =
                $"INSERT INTO {supTableName}_x_{name} (" +
                $"{supTableName}_ID, {(!isForCustomType ? name : name + "_ID")})" +
                $"VALUES ({internalId}, {value});";
            var command = dataHelper.CreateCommand(commandText);
            command.ExecuteNonQuery();
        }


        private int GetNextAutoIncrementForSqlTable(string tableName)
        {
            var command = dataHelper.CreateCommand($"SELECT IDENT_CURRENT('{tableName}');");
            var currentAutoIncrement = command.ExecuteScalar();
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
            var commandText =
                "SELECT CASE WHEN EXISTS" +
                $"(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}') " +
                " THEN 1 ELSE 0 END";
            var command = dataHelper.CreateCommand(commandText);

            return (int) command.ExecuteScalar() == 1;
        }
    }
}

//Can not always map nested enumerables correctly (e.g. lists of different types in a list, but that would most often use an object)
//TODO: Put [] around possible nameing-violations :)
//TODO: Age in Person is 2021 (??)
//TODO: Unterscheiden zwischen 1:n und 1:m I think (???) and think about Ref again