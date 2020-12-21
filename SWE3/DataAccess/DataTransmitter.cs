using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using Serilog;
using SWE3.DataAccess.Interfaces;

namespace SWE3.DataAccess
{
    /// <inheritdoc />
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

        /// <inheritdoc />
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
                "I_AI_ID decimal IDENTITY(1,1), "; //Internal Auto-Increment ID
            
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
            
            logger.Information("Finished Creation.");
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

        /// <inheritdoc />
        public int InsertIntoSqlTable(object instance)
        {
            InsertionQueue.Clear();
            var id =  InsertIntoSqlTableWithRecursion(instance);
            CachingHelper.Set(id, instance);
            return id;
        }

        /// <summary>
        /// See public method 'InsertIntoSqlTable'.
        /// </summary>
        private int InsertIntoSqlTableWithRecursion(object instance)
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
                        continue;
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
                        var objectId = InsertIntoSqlTableWithRecursion(values[i]);
                        InsertIntoSqlHelperTable(table.Name, column.Name, internalId, objectId);
                    }
                    else
                    {
                        logger.Information("Inserting multiple custom-type-based values.");
                        column.Type = column.Type.Replace(MULTIPLE, "");
                        column.Type = column.Type.Replace(CUSTOM, "");
                        foreach (var value in (values[i] as IEnumerable)!)
                        {
                            var objectId = InsertIntoSqlTableWithRecursion(value);
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
                logger.Error("Error for query: " + commandText);
                return -1;
            }
            
            logger.Information("Finished Insertion.");

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
        
        /// <inheritdoc />
        public void DeleteByIdWithReferences(int id, Type type = null, object instance = null)
        {
            var tableName = instance?.GetType().Name ?? type!.Name;
            var commandText =
                "SELECT TABLE_NAME " +
                "FROM INFORMATION_SCHEMA.TABLES " +
                $"WHERE TABLE_NAME LIKE '{tableName}%'";
            var command = dataHelper.CreateCommand(commandText);
            var sqlDataReader = command.ExecuteReader();
            
            //Get all related tables
            var relatedTableNames = new List<string>();
            while (sqlDataReader.Read())
            {
                relatedTableNames.Add((string) sqlDataReader[0]);
            }
            sqlDataReader.Close();

            //Get all references to other objects
            foreach (var helperTableName in relatedTableNames.Where(name => name != tableName))
            {
                commandText =
                    "SELECT * " +
                    $"FROM {helperTableName} " +
                    $"WHERE {tableName + "_ID"} = {id}";
                command.CommandText = commandText;
                sqlDataReader = command.ExecuteReader();
                var referenceIds = new List<decimal>();
                instance ??= Activator.CreateInstance(type);
                var namesAndTypes = new List<(string name, Type type)>();
                instance.GetType().GetProperties().ToList().ForEach(property => namesAndTypes.Add((property.Name, property.PropertyType)));
                var referencedTableName =
                    helperTableName.Substring(helperTableName.IndexOf("_x_", StringComparison.Ordinal) + 3);
                Type referencedType = default;
                foreach (var pair in namesAndTypes.Where(pair => pair.name == referencedTableName))
                {
                    referencedTableName = pair.type.IsDefaultSystemType() ? referencedTableName : pair.type.GetUnderlyingType().Name;
                    referencedType = pair.type.GetUnderlyingType();
                    break;
                }
                var valueIsReferenceId = TableExists(referencedTableName);
                while (sqlDataReader.Read())
                {
                    if (valueIsReferenceId && !referenceIds.Contains((int) sqlDataReader[1]))
                    {
                        referenceIds.Add((int) sqlDataReader[1]);
                    }
                }
                sqlDataReader.Close();
                
                //Delete all references from helper tables
                commandText =
                    $"DELETE FROM {helperTableName} " +
                    $"WHERE {tableName + "_ID"} = {id}";
                command.CommandText = commandText;
                command.ExecuteNonQuery();

                //Delete all referenced objects from their tables
                foreach (var referenceId in referenceIds)
                {
                    DeleteByIdWithReferences(decimal.ToInt32(referenceId), referencedType);
                }
            }

            //Delete base entry
            commandText =
                $"DELETE FROM {tableName} " +
                $"WHERE I_AI_ID = {id}";
            command.CommandText = commandText;
            command.ExecuteNonQuery();
            
            CachingHelper.Remove(tableName,id);
        }

        /// <inheritdoc />
        public int UpdateByIdWithReferences(int id, object instance)
        {
            DeleteByIdWithReferences(id, instance: instance);
            var newId =  InsertIntoSqlTable(instance);
            CachingHelper.Remove(instance.GetType().Name,id);
            return newId;
        }

        /// <summary>
        /// Deletes only the base table and ignores all references.
        /// </summary>
        public void DeleteByIdWithoutReferences(int id, Type type = null, string tableName = null, object instance = null)
        {
            tableName ??= instance?.GetType().Name ?? type!.Name;
            var commandText =
                $"DELETE FROM {tableName} " +
                $"WHERE I_AI_ID = {id}";
            var command = dataHelper.CreateCommand(commandText);
            command.ExecuteNonQuery();
            
            CachingHelper.Remove(tableName, id);
        }

        /// <inheritdoc />
        public void UpdateByIdWithoutReferences(int id, object instance)
        {
            var table = instance.ToTable();
            var values = instance.GetType().GetProperties().Select(property => property.GetValue(instance)).ToArray();
            var commandText =
                $"UPDATE {table.Name} SET ";
            var valueIndices = new List<int>();
            foreach((var column, int i) in table.Columns.Select((column, i) => (column, i)))
            {
                if (!column.Type.Contains(CUSTOM) && !column.Type.Contains(MULTIPLE))
                {
                    commandText += $"{column.Name} = @param{i}, ";
                    valueIndices.Add(i);
                }
            }

            commandText = commandText.Substring(0, commandText.Length - 2) + ";";
            var command = dataHelper.CreateCommand(commandText);

            foreach (var i in valueIndices)
            {
                command.Parameters.Add(new SqlParameter($"@param{i}", values[i]));
            }
            
            command.ExecuteNonQuery();
            
            CachingHelper.Remove(table.Name,id);
        }

        /// <inheritdoc />
        public void UpdateWithSingleParameter(int id, string tableName, string parameterName, dynamic parameterValue)
        {
            var commandText =
                $"UPDATE {tableName} " +
                $"SET {parameterName} = @parameterValue;";
            var command = dataHelper.CreateCommand(commandText);
            command.Parameters.Add(new SqlParameter("@parameterValue", parameterValue));
            Console.WriteLine(command.CommandText);
            command.ExecuteNonQuery();

            var item = CachingHelper.Get<dynamic>(tableName, id);
            PropertyInfo property = item.GetType().GetProperty(parameterName);
            if(property != null) property.SetValue(item,parameterValue,null);
            CachingHelper.Remove(tableName,id);
            CachingHelper.Set(tableName,id,item);
        }

        /// <inheritdoc />
        public int Upsert(object instance)
        {
            var table = instance.ToTable();
            var anyPrimaryKeyColumn = table.Columns.FirstOrDefault(column => column.PrimaryKey) ?? 
                                      table.Columns.FirstOrDefault(column => column.Unique);
            if (anyPrimaryKeyColumn != null)
            {
                var value = instance.GetType().GetProperty(anyPrimaryKeyColumn.Name)?.GetValue(instance, null);
                Console.WriteLine(value?.ToString());
                var commandText = $"SELECT TOP 1 I_AI_ID FROM {table.Name} WHERE {anyPrimaryKeyColumn.Name} = @value;";
                var command = dataHelper.CreateCommand(commandText);
                command.Parameters.Add(new SqlParameter("@value", value));
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    reader.Read();
                    return UpdateByIdWithReferences(decimal.ToInt32((decimal) reader[0]), instance);
                }
            }
            return InsertIntoSqlTable(instance);
        }

        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

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