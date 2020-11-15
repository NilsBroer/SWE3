using System.Collections.Generic;

namespace SWE3.Testing.Classes
{
    public class House
    {
        public Address Location { get; set; }
        
        //public List<Person> Inhabitants { get; set; } //TODO: Acount for infinite recursion (primary key is necessary -> ?)
    }
}