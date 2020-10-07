using System;
using SWE3.BusinessLogic.Entities.ExampleClasses;

namespace SWE3.BusinessLogic.Interfaces
{
    public interface ILogic
    {
        public void testDataBaseAccess();
        public TableObject testTableObjectConversion();
        public void testSqlTableCreate();
        public void testSqlTableInsert();
    }
}