using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Serilog;
using SWE3.DataAccess;
using SWE3.DataAccess.Interfaces;
using SWE3.Testing.Classes;
using ILogger = Serilog.ILogger;

namespace SWE3.Testing
{
    [TestFixture]
    public class DevelopmentTests
    {
        private IDataHelper dataHelper;
        private IDataTransmitter dataTransmitter;
        private IDataReceiver dataReceiver;
        private ILogger logger;
        
        private Address address;
        private Person person;

        [SetUp]
        public void Setup()
        {
            //Setup
            dataHelper = new DataHelper(false);
            logger = new LoggerConfiguration()
                .WriteTo.ColoredConsole()
                .CreateLogger();
            dataTransmitter = new DataTransmitter(dataHelper, logger);
            dataReceiver = new DataReceiver(dataHelper, logger);
            
            //GIVEN
            address = new Address()
            {
                Street = "Menzelstraße",
                HouseNumber = 8,
                PostalCode = 1210,
                City = "Vienna",
                Country = "Austria"
            };
            person = new Person
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
                    Location = address,
                    Inhabitants = new List<Person>()
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
            person.House.Inhabitants.Add(person);
        }
        
        //Basic ---------------------------------------

        [Test]
        public void T01_BasicObjectShouldMapToTableObject()
        {
            //WHEN
            var sut = address.ToTable();

            //THEN
            Assert.AreEqual(sut.Name,address.GetType().Name);
            foreach (var expectedPropertyName in address.GetType().GetProperties()
                .Select(property => property.Name))
            {
                Assert.IsTrue(sut.Columns.Any(column => column.Name.Equals(expectedPropertyName)));
            }
        }
        
        [Test]
        public void T02_BasicObjectShouldMapToSqlTable()
        {
            //GIVEN
            ClearDataBase();

            //WHEN
            dataTransmitter.CreateSqlTableFromShell(address);
            
            //THEN
            Assert.IsTrue(TableExists(address.GetType().Name));
        }
        
        [Test]
        public void T03_BasicObjectShouldInsertIntoSqlTable()
        {
            //GIVEN
            ClearDataBase();
            dataTransmitter.CreateSqlTableFromShell(address);

            //WHEN
            dataTransmitter.InsertIntoSqlTable(address);
            
            //THEN
            Assert.IsTrue(TableHasContent(address.GetType().Name));
        }
        
        [Test]
        public void T04_BasicObjectShouldReturnFromSqlTable()
        {
            //GIVEN
            ClearDataBase();
            dataTransmitter.CreateSqlTableFromShell(address);
            dataTransmitter.InsertIntoSqlTable(address);
            var id = GetAnyExistingIdFromTable(address.GetType().Name);
            
            //WHEN
            var sut = dataReceiver.GetObjectFromTableByInternalId<Address>(id);
            
            //THEN
            Assert.AreEqual(address.GetType(), sut.GetType());
            Assert.AreEqual(address.City, sut.City);
            Assert.AreEqual(address.Country, sut.Country);
            Assert.AreEqual(address.Street, sut.Street);
            Assert.AreEqual(address.HouseNumber, sut.HouseNumber);
            Assert.AreEqual(address.PostalCode, sut.PostalCode);
        }
        
        //Advanced ---------------------------------------

        [Test]
        public void T05_AdvancedObjectShouldMapToTableObject()
        {
            var sut = person.ToTable();

            //THEN
            Assert.AreEqual(sut.Name,person.GetType().Name);
            foreach (var expectedPropertyName in person.GetType().GetProperties().Select(property => property.Name))
            {
                Assert.IsTrue(sut.Columns.Any(column => column.Name.Equals(expectedPropertyName)));
            }
        }

        [Test]
        public void T06_AdvancedObjectShouldMapToSqlTable()
        {
            //GIVEN
            ClearDataBase();
            
            //WHEN
            dataTransmitter.CreateSqlTableFromShell(person);
            
            //THEN
            Assert.IsTrue(TableExists(person.GetType().Name));
            //Assert base-table exists
            
            foreach (var expectedTableName in person.GetType().GetProperties()
                .Where(property => !property.GetType().IsDefaultSystemType() && !property.GetType().IsEnumerable())
                .Select(property => property.Name))
            {
                Assert.IsTrue(TableExists(expectedTableName));
                //Assert sub-tables exist
            }
            
            foreach (var expectedTableName in person.GetType().GetProperties()
                .Where(property => property.GetType().IsEnumerable())
                .Select(property => property.Name))
            {
                Assert.IsTrue(TableExists(person.GetType().Name + "_x_" + expectedTableName));
                //Assert helper-tables exist
            }
        }

        [Test]
        public void T07_AdvancedObjectShouldInsertIntoSqlTable()
        {
            //GIVEN
            ClearDataBase();
            dataTransmitter.CreateSqlTableFromShell(person);
            
            //WHEN
            dataTransmitter.InsertIntoSqlTable(person);
            
            //THEN
            Assert.IsTrue(TableHasContent(person.GetType().Name));  //Assert base-table is not empty
            foreach (var expectedTableName in person.GetType().GetProperties()
                .Where(property => !property.PropertyType.IsDefaultSystemType() && !property.PropertyType.IsEnumerable())
                .Select(property => property.Name))
            {
                Assert.IsTrue(TableHasContent(expectedTableName));
                //Assert sub-tables are not empty
            }
            
            foreach (var expectedTableName in person.GetType().GetProperties()
                .Where(property => property.PropertyType.IsEnumerable())
                .Select(property => property.Name))
            {
                Assert.IsTrue(TableHasContent(person.GetType().Name + "_x_" + expectedTableName));
                //Assert helper-tables is not empty
            }
        }

        [Test]
        public void T08_AdvancedObjectShouldReturnFromSqlTable()
        {
            //GIVEN
            ClearDataBase();
            dataTransmitter.CreateSqlTableFromShell(person);
            dataTransmitter.InsertIntoSqlTable(person);
            var id = GetAnyExistingIdFromTable(person.GetType().Name);
            
            //WHEN
            var sut = dataReceiver.GetObjectFromTableByInternalId<Person>(id);
            
            //THEN
            Assert.AreEqual(person.GetType(), sut.GetType());
            Assert.AreEqual(person.PersonId, sut.PersonId);
            Assert.AreEqual(person.Car.NumberPlate, person.Car.NumberPlate);
            Assert.AreEqual(person.Pets[0].Name, sut.Pets[0].Name);
            Assert.AreEqual(person.Pets[1].Name, sut.Pets[1].Name);
        }

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        private bool TableExists(string tableName)
        {
            var commandText = 
                "SELECT CASE WHEN EXISTS" +
                $"(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}') " +
                " THEN 1 ELSE 0 END";
            var command = dataHelper.CreateCommand(commandText);

            return (int) command.ExecuteScalar() == 1;
        }
        
        private bool TableHasContent(string tableName)
        {
            var commandText = 
                "SELECT CASE WHEN EXISTS" +
                $"(SELECT TOP 1 * FROM {tableName})" + 
                "THEN 1 ELSE 0 END";
            logger.Debug(commandText);
            var command = dataHelper.CreateCommand(commandText);

            return (int) command.ExecuteScalar() == 1;
        }

        private int GetAnyExistingIdFromTable(string tableName)
        {
            var reader = dataHelper.CreateCommand($"SELECT TOP 1 I_AI_ID FROM {tableName}").ExecuteReader();
            reader.Read();
            return decimal.ToInt32((decimal) reader[0]);
        }

        private void ClearDataBase()
        {
            dataHelper.CreateCommand("EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'").ExecuteNonQuery();
            dataHelper.CreateCommand("EXEC sp_MSForEachTable 'DELETE FROM ?'").ExecuteNonQuery();
            dataHelper.CreateCommand("EXEC sp_MSForEachTable 'ALTER TABLE ? CHECK CONSTRAINT ALL'").ExecuteNonQuery();
            
        }
    }
}