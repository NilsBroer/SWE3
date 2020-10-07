using System;
using Serilog;
using SWE3.BusinessLogic.Entities.ExampleClasses;
using SWE3.BusinessLogic.Interfaces;
using SWE3.DataAccess.Interfaces;

namespace SWE3.BusinessLogic
{
    public class Logic : ILogic
    {
        private readonly ILogger logger;
        private readonly IDataHelper dataHelper;
        private readonly ISqlMapper sqlMapper;
        
        private readonly TestObject testObject = new TestObject()
        {
            _smallint = 16, _integer = 32, _bigint = 64, _decimal = 420.69, _bit = true,
            _datetime = new DateTime(2000, 01, 01), _nvarchar = "Bow chicka wow-wow!",
            _sqlVariant = new Person{vorname = "Vincent", nachname = "Variant"}
        };
        
        public Logic(ILogger logger, IDataHelper dataHelper, ISqlMapper sqlMapper)
        {
            this.logger = logger;
            this.dataHelper = dataHelper;
            this.sqlMapper = sqlMapper;
        }

        public void testDataBaseAccess()
        {
            logger.Information(
                "I am the logger and I am logging successfully!" +
                               "\n" +
                               "Retrieving data from database...");
            var data = dataHelper.GetTestData();
            Console.WriteLine($"Data: {data}");
        }

        public TableObject testTableObjectConversion()
        {
            return this.testObject.ToTableObject();
        }

        public void testSqlTableCreate()
        {
            sqlMapper.CreateSqlTable(this.testObject.ToTableObject());
        }

        public void testSqlTableInsert()
        {
            sqlMapper.InsertIntoSqlTable(this.testObject);
        }
    }
}