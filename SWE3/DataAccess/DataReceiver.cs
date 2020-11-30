using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Serilog;
using SWE3.DataAccess.Interfaces;

namespace SWE3.DataAccess
{
    public class DataReceiver : IDataReceiver
    {
        private readonly IDataHelper dataHelper;
        private readonly ILogger logger;

        private const string ADD = "Add";
        
        private static readonly List<(string insert, int iteration)> InsertionQueue = new List<(string,int)>();
        private static int Iteration;
        
        public DataReceiver(IDataHelper dataHelper, ILogger logger)
        {
            this.dataHelper = dataHelper;
            this.logger = logger;
        }
        
        /// <summary>
        /// Gets an object (and subobjects, recursively) with a given ID and maps it to the given type.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="type"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>object of type T</returns>
        public T GetObjectFromTableByInternalId<T>(int id, Type type = null)
        {
            type ??= typeof(T);

            if (InsertionQueue.Contains((type.Name, Iteration)))
            {
                logger.Warning("Recursive reference detected and ignored." + '\n' +
                               "If you need self-references you will have to add them yourself.");
                return (T) Activator.CreateInstance(type);
            }
            InsertionQueue.Add((type.Name,Iteration));
            
            var commandText = $"SELECT * FROM {type.Name} WHERE I_AI_ID = @id";
            var command = dataHelper.CreateCommand(commandText);
            command.Parameters.Add(new SqlParameter("@id", id));
            var sqlDataReader = command.ExecuteReader();
            sqlDataReader.Read(); //Read first (and only) row
            
            var instance = (T) Activator.CreateInstance(type);
            foreach (var property in type.GetProperties())
            {
                if (property.SetMethod == null) continue;
                
                var propertyName = property.Name;
                var propertyType = property.PropertyType;

                if ((!propertyType.IsEnumerable() && propertyType.IsDefaultSystemType()) || propertyType.IsEnum) //Single system-type (found in table)
                {
                    var propertyValue = sqlDataReader.GetValue(sqlDataReader.GetOrdinal(propertyName));
                    type.GetProperty(propertyName)!.SetValue(instance,propertyValue.MakeNullSafe());
                }
                else if (!propertyType.IsEnumerable()) //Single custom-type
                {
                    var subInstance = GetCorrespondingObjectsViaHelperTable(
                        type.Name, propertyName, decimal.ToInt32((decimal) sqlDataReader[0]), propertyType)
                        .FirstOrDefault();
                    type.GetProperty(propertyName)!.SetValue(instance,subInstance.MakeNullSafe());
                }
                else if (propertyType.GetUnderlyingType().IsDefaultSystemType()) //Multiple sytem-types
                {
                    var values = GetCorrespondingValuesViaHelperTable(
                        type.Name, propertyName, decimal.ToInt32((decimal) sqlDataReader[0])).ToList();
                    object enumerable;
                    if(propertyType.IsArray)
                    {
                        enumerable = Array.CreateInstance(propertyType.GetUnderlyingType(), 0);
                        for (var i = 0; i < values.Count; i++)
                        {
                            ((Array) enumerable).SetValue(values[i].MakeNullSafe(),i);
                        }
                    }
                    else 
                    {
                        enumerable = Activator.CreateInstance(propertyType);
                        values.ForEach(val => propertyType.GetMethod(ADD)!
                            .Invoke(enumerable,new[] {val.MakeNullSafe()}));
                    }
                    type.GetProperty(propertyName)!.SetValue(instance,enumerable);
                }
                else //Multiple custom-types
                {
                    var subInstances = GetCorrespondingObjectsViaHelperTable(
                        type.Name, propertyName, decimal.ToInt32((decimal) sqlDataReader[0]), propertyType.GetUnderlyingType())
                        .ToList();
                    
                    var enumerable = new object();
                    if(propertyType.IsArray)
                    {
                        enumerable = Array.CreateInstance(propertyType.GetUnderlyingType(), subInstances.Count);
                        for (var i = 0; i < subInstances.Count; i++)
                        {
                            ((Array) enumerable).SetValue(subInstances[i],i);
                        }
                    }
                    else
                    {
                        try
                        {
                            enumerable = Activator.CreateInstance(propertyType);
                            subInstances.ForEach(inst => propertyType.GetMethod(ADD)!.Invoke(enumerable,new[] {inst}));
                        }
                        catch (Exception e)
                        {
                            logger.Error("Unknown kind of enumerable, error: ", e);
                        }
                    }
                    type.GetProperty(propertyName)!.SetValue(instance,enumerable);
                    Iteration++;
                }
            }
            
            return instance;
        }

        private IEnumerable<object> GetCorrespondingValuesViaHelperTable(string baseTableName, string valueTableName, int baseId)
        {
            var commandText = $"SELECT * " +
                              $"FROM {baseTableName}_x_{valueTableName} " +
                              $"WHERE {baseTableName}_ID = @id";
            var command = dataHelper.CreateCommand(commandText);
            command.Parameters.Add(new SqlParameter("@id", baseId));
            var sqlDataReader = command.ExecuteReader();
            while (sqlDataReader.Read())
            {
                yield return sqlDataReader[1];
            }
        }

        private IEnumerable<object> GetCorrespondingObjectsViaHelperTable(string baseTableName, string objectTableName, int baseId, Type type)
        {
            var commandText = $"SELECT {objectTableName}_ID " +
                              $"FROM {baseTableName}_x_{objectTableName} " +
                              $"WHERE {baseTableName}_ID = @id";
            var command = dataHelper.CreateCommand(commandText);
            command.Parameters.Add(new SqlParameter("@id", baseId));
            var sqlDataReader = command.ExecuteReader();
            
            var objectIds = new List<int>();
            while (sqlDataReader.Read())
            {
                objectIds.Add((int) sqlDataReader[0]);
            }

            var multiple = objectIds.Count > 1;

            foreach (var instance in objectIds.Select(objectId => GetObjectFromTableByInternalId<object>(objectId, type)))
            {
                if (multiple) ++Iteration;
                yield return instance;
            }
        }

        public object GetObjectFromTablePrimaryKeys<T>(string tableName, string[] primaryKeyValues)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<object> GetAllObjectsFromTable<T>(string tableName)
        {
            throw new System.NotImplementedException();
        }
    }
}

//NOTE: Does not work with multi-level arrays and custom enumerables