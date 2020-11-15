using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SWE3.BusinessLogic.Entities;
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
        
        private Person advancedObject;
        private Address basicObject;

        [SetUp]
        public void Setup()
        {
            //Setup
            dataHelper = new DataHelper(false);
            sqlMapper = new SqlMapper(dataHelper);
            
            //Given
            advancedObject = new Person
            {
                PersonId = 1,
                SocialSecurityNumber = "A123456789",
                BirthDate = new DateTime(1996, 05, 20),
                IsEmployed = true,
                Car = new Car
                {
                    Brand = Car.BrandEnum.Ferrari,
                    NumberPlate = "ASDF6969"
                },
                House = new House
                {
                    Location = basicObject = new Address
                    {
                        Street = "Menzelstraße",
                        HouseNumber = 8,
                        PostalCode = 1210,
                        City = "Vienna",
                        Country = "Austria"
                    }
                },
                Pets = new Pet[]
                {
                    new Pet{ Name = "Jimmy", receivedTheirShots = true },
                    new Pet{ Name = "Fridolin" }
                },
                FavoriteNumbers = new List<int>
                {
                    1,3,5,7,11,13,17
                }
            };
        }

        [Test]
        public void BasicObjectShouldMapToTableObject()
        {
            //When
            var table = basicObject.ToTable();
            const string expectedObjectName = "Address";
            var expectedPropertyNames = new List<string>
            {
                "Street", "HouseNumber", "PostalCode", "City", "Country"
            };
            
            //Then
            Assert.AreEqual(table.Name,expectedObjectName);
            foreach (var expectedPropertyName in expectedPropertyNames)
            {
                Assert.IsTrue(table.Columns.Any(column => column.Name.Equals(expectedPropertyName)));
            }
            
            table.Columns.ForEach(column => Console.WriteLine(column.Name + " : " + column.Type));
        }

        [Test]
        public void AdvancedObjectShouldMapToTableObject()
        {
            //When
            var table = advancedObject.ToTable();
            const string expectedObjectName = "Person";

            var expectedPropertyNames = new List<string>
            {
                "PersonId", "SocialSecurityNumber", "Age", "BirthDate", "IsEmployed", "Car", "House", "Pets"
            };
            
            //Then
            Assert.AreEqual(table.Name,expectedObjectName);
            foreach (var expectedPropertyName in expectedPropertyNames)
            {
                Assert.IsTrue(table.Columns.Any(column => column.Name.Equals(expectedPropertyName)));
            }
            
            table.Columns.ForEach(column => Console.WriteLine(column.Name + " : " + column.Type));
        }

        [Test]
        public void BasicTableObjectShouldMapToSqlTable()
        {
            //Given
            DropTableForTesting(basicObject.GetType().Name);

            //When
            sqlMapper.CreateSqlTableFromShell(basicObject);
            
            //Then
            Assert.IsTrue(BasicTableCreationSucceeded());
        }
        
        [Test]
        public void BasicObjectShouldInsertIntoSqlTable()
        {
            //Given
            DropTableForTesting(basicObject.GetType().Name);
            CreateTableForTesting(basicObject.GetType().Name);

            //When
            sqlMapper.InsertIntoSqlTable(basicObject);
            
            //Then
            Assert.IsTrue(BasicValueInsertionSucceeded(basicObject.GetType().Name));
        }

        [Test]
        public void AdvancedObjectShouldMapToSqlTable()
        {
            //Given
            DropTableForTesting(advancedObject.GetType().Name);
            
            //When
            sqlMapper.CreateSqlTableFromShell(advancedObject);
            
            //Then
            Assert.IsTrue(AdvancedTableCreationSucceeded());
        }

        [Test]
        public void AdvancedObjectShouldInsertIntoSqlTable()
        {
            //Given
            DropTableForTesting(advancedObject.GetType().Name);
            CreateTableForTesting(advancedObject.GetType().Name);
            
            //When
            sqlMapper.InsertIntoSqlTable(advancedObject);
            
            //Then
            Assert.IsTrue(AdvancedValueInsertSucceeded());
        }
        
        //~~~~~~~~~~Assertion Helper~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        private bool BasicTableCreationSucceeded()
        {
            return TableExists(basicObject.GetType().Name);
        }
        private bool BasicValueInsertionSucceeded(string tableName)
        {
            var commandText = 
                "SELECT CASE WHEN EXISTS" +
                $"(SELECT 1 FROM {tableName} WHERE I_AI_ID = 1)" + 
                "THEN 1 ELSE 0 END";
            Console.WriteLine(commandText);
            var command = dataHelper.CreateCommand(commandText);

            return (int) command.ExecuteScalar() == 1;
        }
        private bool AdvancedTableCreationSucceeded()
        {
            var expectedTableNames = new List<string>
            {
                advancedObject.GetType().Name,
                "Car",
                "House",
                "Pet",
                "Address"
            };
            var results = new List<bool>();

            foreach (var tableName in expectedTableNames)
            {
                var commandText = 
                    "SELECT CASE WHEN EXISTS" +
                    $"(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}') " +
                    " THEN 1 ELSE 0 END";
                var command = dataHelper.CreateCommand(commandText);

                results.Add((int) command.ExecuteScalar() == 1);
            }

            foreach (var result in results)
            {
                if (result) continue;
                else return false;
            }
            return true;
        }

        private bool AdvancedValueInsertSucceeded()
        {
            return true; //TODO: Implement
        }
        
        private void DropTableForTesting(string tableName)
        {
            if (!TableForTestingExists(tableName)) return;
            
            var commandText = $"DROP TABLE {tableName};";
            var command = dataHelper.CreateCommand(commandText);
            command.ExecuteNonQuery();
        }
        
        //~~~~~~~~~~Execution Helper~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        
        private bool TableExists(string tableName)
        {
            var commandText = 
                "SELECT CASE WHEN EXISTS" +
                $"(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}') " +
                " THEN 1 ELSE 0 END";
            var command = dataHelper.CreateCommand(commandText);

            return (int) command.ExecuteScalar() == 1;
        }

        private void CreateTableForTesting(string tableName)
        {
            if (TableExists(tableName)) return;

            sqlMapper.CreateSqlTableFromShell(tableName == basicObject.GetType().Name ? (object) basicObject : advancedObject);
            //It's weird that this needs a cast but ok :)
        }
        
        private bool TableForTestingExists(string tableName)
        {
            var commandText = 
                "SELECT CASE WHEN EXISTS" +
                $"(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}') " +
                " THEN 1 ELSE 0 END";
            var command = dataHelper.CreateCommand(commandText);

            return (int) command.ExecuteScalar() == 1;
        }
    }
}