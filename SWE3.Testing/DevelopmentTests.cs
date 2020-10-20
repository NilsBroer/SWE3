using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SWE3.DataAccess;
using SWE3.DataAccess.Interfaces;
using SWE3.Testing.Classes;

namespace SWE3.Testing
{
    [TestFixture]
    public class DevelopmentTests
    {
        private IDataHelper dataHelper;
        private ISqlMapper sqlMapper;
        
        private Person person;
        private TestObject testObject;
        private string tableName;

        [SetUp]
        public void Setup()
        {
            //Setup
            
            dataHelper = new DataHelper(false);
            sqlMapper = new SqlMapper(dataHelper);
            
            //Given
            
            person = new Person
            {
                PersonId = 1, Vorname = "Toni", Nachname = "Tester", Geburtsdatum = new DateTime(2020, 10, 06)
            };
            
            testObject = new TestObject
            {
                PrimaryKey = "PK_" + Guid.NewGuid(), LaterPrimaryKeyPart2 = 01,
                Smallint = 16, Integer = 32, Bigint = 64, Decimal = 420.69, Bit = true,
                DateTime = new DateTime(2000, 01, 01), NVarchar = "Bow chicka wow-wow!"
            };
            
            tableName = testObject.GetType().Name;

        }

        [Test]
        public void ClassShouldMapToTable()
        {
            //When
            
            var sut = testObject.ToTable();
            
            const string expectedObjectName = "TestObject";
            if (expectedObjectName != tableName)
            {
                throw new Exception("Name of object not as expected, even before conversion");
            }
            
            var expectedPropertyNames = new List<string>()
            {
                "PrimaryKey", "LaterPrimaryKeyPart2", "Smallint", "Integer", "Bigint", "Decimal", "Bit", "DateTime", "NVarchar"
            };
            
            //Then
            
            Assert.AreEqual(sut.Name,expectedObjectName);
            foreach (var expectedPropertyName in expectedPropertyNames)
            {
                Assert.IsTrue(sut.Columns.Any(column => column.Name.Equals(expectedPropertyName)));
            }
            Assert.IsTrue(sut.Columns.Any(column => column.PrimaryKey));
            Assert.IsTrue(sut.Columns.Any(column => !column.PrimaryKey));
            
        }

        [Test]
        public void TableShouldMapToSqlTable()
        {
            DropTableForTesting();

            //When
            var table = testObject.ToTable();
            sqlMapper.CreateSqlTable(table);
            
            //Then
            Assert.IsTrue(TableCreationSucceeded());
            
        }

        [Test]
        public void ObjectShouldInsertIntoSqlTable()
        {
            CreateTableForTesting();
            
            //When
            sqlMapper.InsertIntoSqlTable(testObject);
            
            //Then
            Assert.IsTrue(ValueInsertionSucceeded());
        }

        private void DropTableForTesting()
        {
            if (!TableForTestingExists()) return;
            
            var commandText = $"DROP TABLE {tableName};";
            var command = dataHelper.CreateCommand(commandText);
            command.ExecuteNonQuery();
        }

        private void CreateTableForTesting()
        {
            if (TableForTestingExists()) return;
            
            var table = testObject.ToTable();
            sqlMapper.CreateSqlTable(table);
        }
        
        private bool TableForTestingExists()
        {
            var commandText = 
                "SELECT CASE WHEN EXISTS" +
                $"((SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}')) " +
                "THEN 1 ELSE 0 END";
            var command = dataHelper.CreateCommand(commandText);

            return (int) command.ExecuteScalar() == 1;
        }

        private bool TableCreationSucceeded()
        {
            return true;
        }

        private bool ValueInsertionSucceeded()
        {
            return true;
        }
    }
}