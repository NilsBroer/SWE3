using System;
using System.Collections.Generic;

namespace SWE3.DataAccess.Interfaces
{
    public interface IDataReceiver
    {
        public T GetObjectByInternalId<T>(int id, Type type = null) where T : class;
        public IEnumerable<T> GetAllObjectsFromTable<T>(string tableName = null) where T : class;
    }
}