using System.Collections.Generic;
using SWE3.BusinessLogic.Entities;

namespace SWE3.Testing.Classes
{
    public class House : Cloneable
    {
        public Address Location { get; set; }
        
        public List<Person> Inhabitants { get; set; }
    }
}