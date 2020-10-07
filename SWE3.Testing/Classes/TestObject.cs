using System;

namespace SWE3.Testing.Classes
{
    public class TestObject
    {
        public short _smallint { get; set; }
        public int _integer { get; set; }
        public long _bigint { get; set; }
        public double _decimal { get; set; }
        public string _nvarchar { get; set; }
        public DateTime _datetime { get; set; }
        public bool _bit { get; set; }
        public object _sqlVariant { get; set; }
    }
}