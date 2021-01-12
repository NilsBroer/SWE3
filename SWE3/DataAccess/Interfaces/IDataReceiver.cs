using System;
using System.Collections.Generic;

namespace SWE3.DataAccess.Interfaces
{
    /// <summary>
    /// Receives (and transforms) data from an sql-database
    /// </summary>
    public interface IDataReceiver
    {
        /// <summary>
        /// Gets an object (and subobjects, recursively) with a given ID and maps it to the given type.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="type"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>object of type T</returns>
        public T GetObjectByInternalId<T>(int id, Type type = null) where T : class;
        
        /// <summary>
        /// Gets all objects (full) from a certain table
        /// </summary>
        /// <param name="tableName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>collection of objects of type T</returns>
        public IEnumerable<T> GetAllObjectsFromTable<T>(string tableName = null) where T : class;

        /// <summary>
        /// Gets a multilayered list, representing all data like a multi-layered array, as such:
        /// data[row][col]
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public List<List<object>> GetDataByCustomQuery(string query);
    }
}