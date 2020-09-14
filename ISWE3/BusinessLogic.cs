using System;
using ISWE3.Utilities;
using Serilog;

namespace ISWE3
{
    public class BusinessLogic : IBusinessLogic
    {
        private readonly ILogger logger;
        private readonly IDataAccess dataAccess;
        public BusinessLogic(ILogger logger, IDataAccess dataAccess)
        {
            this.logger = logger;
            this.dataAccess = dataAccess;
        }
        public void MyFunction()
        {
            var data = dataAccess.GetData();
            logger.Information("I am the logger and I am logging!");
            Console.WriteLine($"Data: {data}");
        }
    }
}