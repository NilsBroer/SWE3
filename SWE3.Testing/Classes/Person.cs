using System;
using SWE3.BusinessLogic.Entities;

namespace SWE3.Testing.Classes
{
    public class Person
    {
        [PrimaryKey] public int PersonId;
        public string Vorname;
        public string Nachname;
        public DateTime Geburtsdatum;
    }
}