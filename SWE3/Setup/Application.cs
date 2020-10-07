using System;
using System.Linq;
using SWE3.BusinessLogic.Entities.ExampleClasses;
using SWE3.BusinessLogic.Interfaces;

namespace SWE3.Setup
{
    public class Application : IApplication
    {
        private readonly ILogic logic;

        public Application(ILogic logic)
        {
            this.logic = logic;
        }

        public void Run()
        {
            //logic.testDataBaseAccess();
            //logic.testTableObjectConversion();
            //logic.testSqlTableCreate();
            logic.testSqlTableInsert();
        }
    }
}
