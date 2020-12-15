using SWE3.BusinessLogic.Entities;

namespace SWE3.Testing.Classes
{
    public class Address
    {
        [PrimaryKey] public string AddressIdentifier { get; set; }
        public string Street { get; set; }
        public int HouseNumber { get; set; }
        public int PostalCode { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }
}