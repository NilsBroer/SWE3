using System;
using System.Collections.Generic;

namespace SWE3.DataAccess.Interfaces
{
    public interface IDataReceiver
    {
        public T GetObjectFromTableByInternalId<T>(int id, Type type = null);
    }
}