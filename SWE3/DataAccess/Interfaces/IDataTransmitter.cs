using System;
using SWE3.BusinessLogic;
using SWE3.BusinessLogic.Entities;

namespace SWE3.DataAccess.Interfaces
{
    /// <summary>
    /// Transmitts data from C#-code to SQL(-tables).
    /// Class --> table (Create)
    /// Instance --> Entry (Insert)
    /// </summary>
    public interface IDataTransmitter
    {
        /// <summary>
        /// Builds a table-object and from that a new SQL-table according to the properties of the given class.
        /// Object can be empty, as only the shell (class-properties) is required.
        /// </summary>
        public void CreateSqlTableFromShell(object shell);
        
        /// <summary>
        /// Inserts the values held by an object (instance) into an already existing sql-table
        /// </summary>
        /// <returns>ID upon success (>= 1), 0 when redundant, -1 upon failure</returns>
        public int InsertIntoSqlTable(object instance);
        
        /// <summary>
        /// Deletes an entry by id
        /// and tries to delete all related entries (like sub-objects)
        /// </summary>
        public void DeleteByIdWithReferences(int id,  Type type = null, object instance = null);

        /// <summary>
        /// Deletes an entry by id
        /// only referencing the table of the entry itself
        /// </summary>
        public void DeleteByIdWithoutReferences(int id, Type type = null, string tableName = null, object instance = null);
        
        /// <summary>
        /// Updates an entry by id
        /// and tries to update all related entries
        /// </summary>
        /// <returns>ID upon success (>= 1), -1 upon failure</returns>
        public int UpdateByIdWithReferences(int id, object instance);
        
        /// <summary>
        /// Updates an entry by id
        /// only referencing the table of the entry itself
        /// The object is deleted from cache
        /// </summary>
        public void UpdateByIdWithoutReferences(int id, object instance);
        
        /// <summary>
        /// Updates a single entry, ignoring everything else
        /// </summary>
        public void UpdateWithSingleParameter(int id, string tableName, string parameterName, dynamic parameterValue);
        
        /// <summary>
        /// Decides whether the given object already existed in the database prio to the function call
        /// if it existed, the object is updated (with references)
        /// if it didn't, the object is inserted fully
        /// </summary>
        /// <returns>ID, as declared in the two used functions above</returns>
        public int Upsert(object instance);
    }
}