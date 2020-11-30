using System;
using System.Runtime.CompilerServices;
using SWE3.BusinessLogic.Entities;

namespace SWE3.Testing.Classes
{
    public class Car
    {
        public BrandEnum Brand { get; set; }
        
        [PrimaryKey] public string NumberPlate { get; set; }

        public enum BrandEnum
        {
            Toyota,
            Opel,
            Suzuki,
            Mazda,
            Ferrari
        }
        
    }

    public static class ExtensionMethods
    {
        public static string Name(this Car.BrandEnum brand)
        {
            return brand switch
            {
                Car.BrandEnum.Toyota => "Toyota",
                Car.BrandEnum.Opel => "Opel",
                Car.BrandEnum.Suzuki => "Suzuki",
                Car.BrandEnum.Mazda => "Mazda",
                Car.BrandEnum.Ferrari => "Ferrari",
                _ => "Unknown"
            };
        }
    }
}