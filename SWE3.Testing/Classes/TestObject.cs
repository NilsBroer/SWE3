using System;
using SWE3.BusinessLogic.Entities;

namespace SWE3.Testing.Classes
{
    public class TestObject
    {
        [PrimaryKey] public string PrimaryKey { get; set; }
        [NotNull] public int LaterPrimaryKeyPart2 { get; set; }
        public short Smallint { get; set; }
        public int Integer { get; set; }
        public long Bigint { get; set; }
        public double Decimal { get; set; }
        public string NVarchar { get; set; }
        public DateTime DateTime { get; set; }
        public bool Bit { get; set; }
        
        //TODO: !IMPORTANT - Do not use SqlVariant for Objects, create another table instead (recursion)
    }
}