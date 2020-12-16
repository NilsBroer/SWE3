using SWE3.BusinessLogic.Entities;

namespace SWE3.Testing.Classes
{
    public class Pet : Cloneable
    {
        public string Name { get; set; }
        public bool? receivedTheirShots { get; set; }
    }
}