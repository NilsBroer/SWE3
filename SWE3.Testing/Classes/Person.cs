using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using SWE3.BusinessLogic.Entities;

namespace SWE3.Testing.Classes
{
    public class Person
    {
        [PrimaryKey] public int PersonId { get; set; }
        [Unique] [NotNullable] public string SocialSecurityNumber { get; set; }
        
        public DateTime BirthDate { get; set; }
        public int Age { get; set; }
        public bool IsEmployed { get; set; }

        public Car Car { get; set; }
        public House House { get; set; }

        public string[] Pets { get; set; }
        
        public List<int> FavoriteNumbers { get; set; }

        public object AlwaysNull = null;

        public Person()
        {
            this.Age = (DateTime.Now - this.BirthDate).Days / 365;
        }

    }
}