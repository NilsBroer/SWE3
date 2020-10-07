using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SWE3.Testing.Classes;

namespace SWE3.Testing
{
    [TestFixture]
    public class DevelopmentTests
    {
        private Person person;
        private TestObject testObject;

        [SetUp]
        public void Setup() //Given
        {
            person = new Person
            {
                personid = 1, vorname = "Toni", nachname = "Tester", geburtsdatum = new DateTime(2020, 10, 06)
            };
            testObject = new TestObject
            {
                _smallint = 16, _integer = 32, _bigint = 64, _decimal = 420.69, _bit = true,
                _datetime = new DateTime(2000, 01, 01), _nvarchar = "Bow chicka wow-wow!", _sqlVariant = person
            };
        }

        [Test]
        public void ClassShouldMapToTableObject()
        {
            //When
            var sut = testObject.ToTableObject();
            var propertyNames = new List<string>()
            {
                "_smallint", "_integer", "_bigint", "_decimal", "_bit", "_datetime", "_nvarchar", "_sqlVariant"
            };
            
            //Then
            Assert.AreEqual(sut.tableName,"TestObject");
            Assert.AreEqual(sut.tableName,testObject.GetType().Name);
            foreach (var propertyName in propertyNames)
            {
                Assert.IsTrue(sut.columns.Any(column => column.name!.Equals(propertyName)));
            }
            Assert.Contains("_decimal",propertyNames);
        }

        [Test]
        public void TableObjectShouldMapToSqlTable()
        {
            
        }
    }
}