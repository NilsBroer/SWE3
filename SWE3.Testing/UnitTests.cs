using System;
using System.Collections.Generic;
using System.Linq;
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
                AddressIdentifier = "ADD1",
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
        public void T001_BasicObjectShouldMapToTableObject()
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
        public void T002_BasicObjectShouldMapToSqlTable()
        {
            //GIVEN
            dataHelper.ClearDatabase();

            //WHEN
            dataTransmitter.CreateSqlTableFromShell(address);
            
            //THEN
            Assert.IsTrue(TableExists(address.GetType().Name));
        }
        
        [Test]
        public void T003_BasicObjectShouldInsertIntoSqlTable()
        {
            //GIVEN
            dataHelper.ClearDatabase();
            dataTransmitter.CreateSqlTableFromShell(address);

            //WHEN
            dataTransmitter.InsertIntoSqlTable(address);
            
            //THEN
            Assert.IsTrue(TableHasContent(address.GetType().Name));
        }
        
        [Test]
        public void T004_BasicObjectShouldReturnFromSqlTable()
        {
            //GIVEN
            dataHelper.ClearDatabase();
            dataTransmitter.CreateSqlTableFromShell(address);
            var id = dataTransmitter.InsertIntoSqlTable(address);

            //WHEN
            var sut = dataReceiver.GetObjectByInternalId<Address>(id);
            
            //THEN
            Assert.AreEqual(address.GetType(), sut.GetType());
            Assert.AreEqual(address.City, sut.City);
            Assert.AreEqual(address.Country, sut.Country);
            Assert.AreEqual(address.Street, sut.Street);
            Assert.AreEqual(address.HouseNumber, sut.HouseNumber);
            Assert.AreEqual(address.PostalCode, sut.PostalCode);
        }

        [Test]
        public void T005_BasicObjectShouldDeleteWithoutReferences()
        {
            //GIVEN
            dataHelper.ClearDatabase();
            dataTransmitter.CreateSqlTableFromShell(address);
            var id = dataTransmitter.InsertIntoSqlTable(address);
            
            //WHEN
            dataTransmitter.DeleteByIdWithoutReferences(id, instance: address);
            var sut = dataReceiver.GetObjectByInternalId<Address>(id);
            
            //THEN
            Assert.AreEqual(null, sut);
        }

        [Test]
        public void T006_BasicObjectShouldDeleteWithReferences()
        {
            //GIVEN
            dataHelper.ClearDatabase();
            dataTransmitter.CreateSqlTableFromShell(address);
            var id = dataTransmitter.InsertIntoSqlTable(address);

            //WHEN
            dataTransmitter.DeleteByIdWithReferences(id, instance: address);
            var sut = dataReceiver.GetObjectByInternalId<Address>(id);
            
            //THEN
            Assert.AreEqual(null, sut);
        }

        [Test]
        public void T007_BasicObjectShouldUpdateWithoutReferences()
        {
            //GIVEN
            dataHelper.ClearDatabase();
            dataTransmitter.CreateSqlTableFromShell(address);
            var id = dataTransmitter.InsertIntoSqlTable(address);
            
            var changedAddress = address;
            var expectedCity = changedAddress.City = "a brand new city";
            var expectedPostalCode = changedAddress.PostalCode = 42069;
            
            //WHEN
            dataTransmitter.UpdateByIdWithoutReferences(id, address);
            var sut = dataReceiver.GetObjectByInternalId<Address>(id);
            
            //THEN
            Assert.AreEqual(expectedCity,sut.City);
            Assert.AreEqual(expectedPostalCode,sut.PostalCode);
        }

        [Test]
        public void T008_BasicObjectShouldUpdateWithReferences()
        {
            //GIVEN
            dataHelper.ClearDatabase();
            dataTransmitter.CreateSqlTableFromShell(address);
            var id = dataTransmitter.InsertIntoSqlTable(address);
            
            var changedAddress = address;
            var expectedCity = changedAddress.City = "a brand new city";
            var expectedPostalCode = changedAddress.PostalCode = 42069;
            
            //WHEN
            id = dataTransmitter.UpdateByIdWithReferences(id, changedAddress);
            var sut = dataReceiver.GetObjectByInternalId<Address>(id);
            
            //THEN
            Assert.AreEqual(expectedCity,sut.City);
            Assert.AreEqual(expectedPostalCode,sut.PostalCode);
        }
        
        [Test]
        public void T009_BasicObjectShouldUpdateWithSingleParameter()
        {
            //GIVEN
            dataHelper.ClearDatabase();
            dataTransmitter.CreateSqlTableFromShell(address);
            var id = dataTransmitter.InsertIntoSqlTable(address);

            var expectedCity = "updated city";
            
            //WHEN
            dataTransmitter.UpdateWithSingleParameter(tableName: address.GetType().Name, id: id,
                parameterName: nameof(address.City), parameterValue: expectedCity);
            var sut = dataReceiver.GetObjectByInternalId<Address>(id);
            
            //THEN
            Assert.AreEqual(expectedCity,sut.City);
            Assert.AreEqual(address.PostalCode,sut.PostalCode);
            Assert.AreNotEqual(expectedCity,address.City);
        }

        [Test]
        public void T010_BasicObjectShouldUpsertCorrectly()
        {
            //GIVEN
            dataHelper.ClearDatabase();
            dataTransmitter.CreateSqlTableFromShell(address);
            
            var differentAddress = new Address { AddressIdentifier = "anything else" };
            
            dataTransmitter.Upsert(differentAddress);
            var id = dataTransmitter.Upsert(address);

            var expectedAddressExceptForCity = dataReceiver.GetObjectByInternalId<Address>(id);
            
            var expectedCity = address.City = "updated city";
            
            //WHEN
            id = dataTransmitter.Upsert(address);
            var sut = dataReceiver.GetObjectByInternalId<Address>(id);
            
            //THEN
            Assert.AreEqual(expectedAddressExceptForCity.AddressIdentifier,sut.AddressIdentifier);
            Assert.AreEqual(expectedAddressExceptForCity.PostalCode, sut.PostalCode);
            Assert.AreNotEqual(expectedAddressExceptForCity,sut.City);
            Assert.AreEqual(expectedCity,sut.City);
            Assert.AreNotEqual(differentAddress.AddressIdentifier,sut.AddressIdentifier);
        }
        
        //Advanced ---------------------------------------

        [Test]
        public void T101_AdvancedObjectShouldMapToTableObject()
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
        public void T102_AdvancedObjectShouldMapToSqlTable()
        {
            //GIVEN
            dataHelper.ClearDatabase();
            
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
        public void T103_AdvancedObjectShouldInsertIntoSqlTable()
        {
            //GIVEN
            dataHelper.ClearDatabase();
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
        public void T104_AdvancedObjectShouldReturnFromSqlTable()
        {
            //GIVEN
            dataHelper.ClearDatabase();
            dataTransmitter.CreateSqlTableFromShell(person);
            var id = dataTransmitter.InsertIntoSqlTable(person);

            //WHEN
            var sut = dataReceiver.GetObjectByInternalId<Person>(id);
            
            //THEN
            Assert.AreEqual(person.GetType(), sut.GetType());
            Assert.AreEqual(person.PersonId, sut.PersonId);
            Assert.AreEqual(person.Car.NumberPlate, person.Car.NumberPlate);
            Assert.AreEqual(person.Pets[0].Name, sut.Pets[0].Name);
            Assert.AreEqual(person.Pets[1].Name, sut.Pets[1].Name);
        }

        [Test]
        public void T105_AdvancedObjectShouldDeleteWithoutReferences()
        {
            //GIVEN
            dataHelper.ClearDatabase();
            dataTransmitter.CreateSqlTableFromShell(person);
            var id = dataTransmitter.InsertIntoSqlTable(person);
            
            //WHEN
            dataTransmitter.DeleteByIdWithoutReferences(id, instance: person);
            var sut = dataReceiver.GetObjectByInternalId<Person>(id);
            
            //THEN
            Assert.AreEqual(null, sut);
            Assert.True(TableHasContent("Person_x_FavoriteNumbers"));
            Assert.True(TableHasContent("Person_x_Pets"));
            Assert.True(TableHasContent("Pet"));
        }
        
        [Test]
        public void T106_AdvancedObjectShouldDeleteWithReferences()
        {
            //GIVEN
            dataHelper.ClearDatabase();
            dataTransmitter.CreateSqlTableFromShell(person);
            var id = dataTransmitter.InsertIntoSqlTable(person);

            //WHEN
            dataTransmitter.DeleteByIdWithReferences(id, instance: person);
            var sut = dataReceiver.GetObjectByInternalId<Person>(id);
            
            //THEN
            Assert.AreEqual(null, sut);
            Assert.False(TableHasContent("Person_x_FavoriteNumbers"));
            Assert.False(TableHasContent("Person_x_Pets"));
            Assert.False(TableHasContent("Pet"));
        }
        
        [Test]
        public void T107_AdvancedObjectShouldUpdateWithoutReferences()
        {
            //GIVEN
            dataHelper.ClearDatabase();
            dataTransmitter.CreateSqlTableFromShell(person);
            var id = dataTransmitter.InsertIntoSqlTable(person);
            var updatedPerson = person;
            var expectedPersonId = updatedPerson.PersonId = 42069;
            var notExpectedNumbers = updatedPerson.FavoriteNumbers = new List<int> {1,2,3,4,5};
            var expectedSSN = updatedPerson.SocialSecurityNumber = "brand new ssn";
            
            //WHEN
            dataTransmitter.UpdateByIdWithoutReferences(id, updatedPerson);
            var sut = dataReceiver.GetObjectByInternalId<Person>(id);
            
            //THEN
            Assert.AreEqual(expectedPersonId,sut.PersonId);
            CollectionAssert.AreNotEqual(notExpectedNumbers,sut.FavoriteNumbers);
            Assert.AreEqual(expectedSSN,sut.SocialSecurityNumber);
        }

        [Test]
        public void T108_AdvancedObjectShouldUpdateWithReferences()
        {
            //GIVEN
            dataHelper.ClearDatabase();
            dataTransmitter.CreateSqlTableFromShell(person);
            var id = dataTransmitter.InsertIntoSqlTable(person);
            var updatedPerson = person;
            var expectedPersonId = updatedPerson.PersonId = 42069;
            var expectedNumbers = updatedPerson.FavoriteNumbers = new List<int> {1,2,3,4,5};
            var expectedSSN = updatedPerson.SocialSecurityNumber = "brand new ssn";

            //WHEN
            id = dataTransmitter.UpdateByIdWithReferences(id, updatedPerson);
            var sut = dataReceiver.GetObjectByInternalId<Person>(id);

            //THEN
            Assert.AreEqual(expectedPersonId,sut.PersonId);
            CollectionAssert.AreEqual(expectedNumbers,sut.FavoriteNumbers);
            Assert.AreEqual(expectedSSN,sut.SocialSecurityNumber);
        }
        
        [Test]
        public void T109_AdvancedObjectShouldUpdateWithSingleParameter()
        {
            //GIVEN
            dataHelper.ClearDatabase();
            dataTransmitter.CreateSqlTableFromShell(person);
            var id = dataTransmitter.InsertIntoSqlTable(person);

            var expectedPersonID = 2020;
            
            //WHEN
            dataTransmitter.UpdateWithSingleParameter(id, person.GetType().Name, nameof(person.PersonId), expectedPersonID);
            var sut = dataReceiver.GetObjectByInternalId<Person>(id);
            
            //THEN
            Assert.AreEqual(expectedPersonID,sut.PersonId);
            Assert.AreEqual(person.SocialSecurityNumber,sut.SocialSecurityNumber);
            CollectionAssert.AreEqual(person.FavoriteNumbers,sut.FavoriteNumbers);
            Assert.AreEqual(person.House.Location.City,sut.House.Location.City);
            Assert.AreNotEqual(expectedPersonID,person.PersonId);
        }

        [Test]
        public void T110_AdvancedObjectShouldUpsertCorrectly()
        {
            //TODO: Implement
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
    }
}